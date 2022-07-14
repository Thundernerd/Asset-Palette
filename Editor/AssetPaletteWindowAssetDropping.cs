using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using GameObjectExtensions = RoyTheunissen.AssetPalette.Extensions.GameObjectExtensions;
using Object = UnityEngine.Object;

namespace RoyTheunissen.AssetPalette
{
    public partial class AssetPaletteWindow
    {
        [NonSerialized] private readonly List<Object> draggedObjectsToProcess = new List<Object>();
        [NonSerialized] private readonly List<PaletteEntry> entriesToAddFromDraggedAssets = new List<PaletteEntry>();
        
        private PaletteEntry GetEntryForAsset(Object asset)
        {
            foreach (PaletteEntry entry in GetEntries())
            {
                if (entry is PaletteAsset paletteAsset && paletteAsset.Asset == asset)
                    return entry;
            }
            return null;
        }
        
        private bool HasEntryForAsset(Object asset)
        {
            return GetEntryForAsset(asset) != null;
        }

        private void HandleAssetDroppingInEntryPanel()
        {
            Event @event = Event.current;
            if (@event.type != EventType.DragUpdated && @event.type != EventType.DragPerform)
                return;

            // NOTE: Ignore assets that are being dragged OUT of the entries panel as opposed to being dragged INTO it.
            bool isValidDrag = isMouseInEntriesPanel && DragAndDrop.objectReferences.Length > 0 &&
                               DragAndDrop.GetGenericData(EntryDragGenericDataType) == null;
            if (!isValidDrag)
                return;

            DragAndDrop.AcceptDrag();
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (@event.type != EventType.DragPerform)
                return;

            HandleAssetDropping(DragAndDrop.objectReferences);
        }

        private void HandleAssetDropping(Object[] objectsToProcess)
        {
            // Determine what entries are to be added as a result of these assets being dropped. Note that we may have
            // to ask the user how they want to handle certain special assets like folders. Because of a Unity bug this
            // means that processing will stop, a context menu will be displayed, two frames will have to be waited,
            // and THEN processing can resume. This bug doesn't seem to happen with Dialogs, only context menus. I
            // prefer to use context menus regardless because it's faster and less jarring for the user.
            draggedObjectsToProcess.Clear();
            draggedObjectsToProcess.AddRange(objectsToProcess);
            entriesToAddFromDraggedAssets.Clear();
            ProcessDraggedObjects();
        }

        private void ProcessDraggedObjects()
        {
            while (draggedObjectsToProcess.Count > 0)
            {
                Object draggedObject = draggedObjectsToProcess[0];

                TryCreateEntriesForDraggedObject(draggedObject, out bool needsToAskForUserInputFirst);

                if (needsToAskForUserInputFirst)
                    return;

                // We processed it!
                draggedObjectsToProcess.RemoveAt(0);
            }

            AddEntriesFromDraggedAssets();
        }

        private void TryCreateEntriesForDraggedObject(Object draggedObject, out bool needsToAskForUserInputFirst)
        {
            needsToAskForUserInputFirst = false;

            string path = AssetDatabase.GetAssetPath(draggedObject);
            
            // If a folder is dragged in, add its contents.
            if (AssetDatabase.IsValidFolder(path))
            {
                CreateEntriesForFolderContents(draggedObject);
                return;
            }

            // Basically any Object is fine as long as it's not a scene GameObject.
            if ((!(draggedObject is GameObject go) || GameObjectExtensions.IsPrefab(go)) &&
                !HasEntryForAsset(draggedObject))
            {
                entriesToAddFromDraggedAssets.Add(new PaletteAsset(draggedObject));
            }
        }

        private void AddEntriesFromDraggedAssets()
        {
            if (entriesToAddFromDraggedAssets.Count == 0)
                return;
            
            ClearEntrySelection();
            AddEntries(entriesToAddFromDraggedAssets);
            entriesToAddFromDraggedAssets.Clear();

            Repaint();
        }

        private void CreateEntriesForFolderContents(Object folder)
        {
            string path = AssetDatabase.GetAssetPath(folder);

            // Find all the assets within this folder.
            List<string> assetsInDraggedFolder = new List<string>();
            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            string draggedFolderPath = path + Path.AltDirectorySeparatorChar;
            foreach (string assetPath in allAssetPaths)
            {
                if (!assetPath.StartsWith(draggedFolderPath) || AssetDatabase.IsValidFolder(assetPath))
                    continue;

                assetsInDraggedFolder.Add(assetPath);
            }

            assetsInDraggedFolder.Sort();

            for (int i = 0; i < assetsInDraggedFolder.Count; i++)
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetsInDraggedFolder[i]);
                if (HasEntryForAsset(asset))
                    continue;

                entriesToAddFromDraggedAssets.Add(new PaletteAsset(asset));
            }
        }
    }
}
