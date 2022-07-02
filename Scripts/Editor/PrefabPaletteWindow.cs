using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RoyTheunissen.PrefabPalette
{
    /// <summary>
    /// Helps organize collections of prefabs and drag them into scenes quickly.
    /// </summary>
    public class PrefabPaletteWindow : EditorWindow
    {
        [Serializable]
        private class PrefabEntry
        {
            public const int TextureSize = 100;
            
            [SerializeField] private GameObject prefab;
            public GameObject Prefab => prefab;

            [NonSerialized] private Texture2D cachedPreviewTexture;
            [NonSerialized] private bool didCachePreviewTexture;
            public Texture2D PreviewTexture
            {
                get
                {
                    if (!didCachePreviewTexture)
                    {
                        didCachePreviewTexture = true;

                        string path = AssetDatabase.GetAssetPath(Prefab.GetInstanceID());
                        cachedPreviewTexture = Editor.RenderStaticPreview(path, null, TextureSize, TextureSize);
                    }
                    return cachedPreviewTexture;
                }
            }

            public bool IsValid => Prefab != null;

            [NonSerialized] private Editor cachedEditor;
            [NonSerialized] private bool didCacheEditor;
            private Editor Editor
            {
                get
                {
                    if (!didCacheEditor)
                    {
                        didCacheEditor = true;
                        Editor.CreateCachedEditor(Prefab, null, ref cachedEditor);
                    }
                    return cachedEditor;
                }
            }

            public PrefabEntry(GameObject prefab)
            {
                this.prefab = prefab;
            }
        }

        private readonly List<PrefabEntry> prefabsToDisplay = new List<PrefabEntry>();
        
        private readonly List<GameObject> draggedPrefabs = new List<GameObject>();
        
        private Vector2 prefabPreviewsScrollPosition;
        
        [NonSerialized] private GUIStyle cachedPrefabPreviewTextStyle;
        [NonSerialized] private bool didCachePrefabPreviewTextStyle;
        private GUIStyle PrefabPreviewTextStyle
        {
            get
            {
                if (!didCachePrefabPreviewTextStyle)
                {
                    didCachePrefabPreviewTextStyle = true;
                    cachedPrefabPreviewTextStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                    {
                        alignment = TextAnchor.LowerCenter
                    };
                }
                return cachedPrefabPreviewTextStyle;
            }
        }
        
        [MenuItem ("Window/General/Prefab Palette")]
        public static void Init() 
        {
            GetWindow<PrefabPaletteWindow>(false, "Prefab Palette");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Drag prefabs here.");

            DropAreaGUI();

            DrawPrefabs();
        }

        private void DrawPrefabs()
        {
            prefabPreviewsScrollPosition = GUILayout.BeginScrollView(prefabPreviewsScrollPosition, "Box");
            EditorGUILayout.BeginHorizontal();
            
            for (int i = 0; i < prefabsToDisplay.Count; i++)
            {
                PrefabEntry prefab = prefabsToDisplay[i];
                
                // Purge invalid entries.
                if (!prefab.IsValid)
                {
                    prefabsToDisplay.RemoveAt(i);
                    i--;
                    continue;
                }

                Rect rect = GUILayoutUtility.GetRect(
                    0, 0, GUILayout.Width(PrefabEntry.TextureSize), GUILayout.Height(PrefabEntry.TextureSize));
                if (prefab.PreviewTexture != null)
                    EditorGUI.DrawPreviewTexture(rect, prefab.PreviewTexture, null, ScaleMode.ScaleToFit, 0.0f);
                else
                {
                    const float brightness = 0.325f;
                    EditorGUI.DrawRect(rect, new Color(brightness, brightness, brightness));
                }
                EditorGUI.LabelField(rect, prefab.Prefab.name, PrefabPreviewTextStyle);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        private bool HasEntry(GameObject prefab)
        {
            foreach (PrefabEntry prefabEntry in prefabsToDisplay)
            {
                if (prefabEntry.Prefab == prefab)
                    return true;
            }
            return false;
        }

        private void DropAreaGUI()
        {
            Event @event = Event.current;
            switch (@event.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
//                    if (!position.Contains(@event.mousePosition))
//                        return;

                    DragAndDrop.AcceptDrag();

                    // Find dragged prefabs.
                    draggedPrefabs.Clear();
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is GameObject go && go.IsPrefab())
                        {
                            draggedPrefabs.Add(go);
                            continue;
                        }

                        string path = AssetDatabase.GetAssetPath(draggedObject);
                        if (AssetDatabase.IsValidFolder(path))
                        {
                            string[] files = Directory.GetFiles(path, "*.prefab", SearchOption.AllDirectories);
                            for (int i = 0; i < files.Length; i++)
                            {
                                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(files[i]);
                                draggedPrefabs.Add(prefab);
                            }

                            continue;
                        }
                        
                        // Unhandled dragged object. Probably a different asset, like a texture.
                    }

                    DragAndDrop.visualMode = draggedPrefabs.Count > 0
                        ? DragAndDropVisualMode.Copy
                        : DragAndDropVisualMode.Rejected;

                    if (@event.type == EventType.DragPerform)
                    {
                        foreach (GameObject draggedPrefab in draggedPrefabs)
                        {
                            Debug.Log($"Dragged prefab {draggedPrefab.name}");
                            
                            if (HasEntry(draggedPrefab))
                                continue;
                            
                            PrefabEntry entry = new PrefabEntry(draggedPrefab);
                            prefabsToDisplay.Add(entry);
                        }
                    }
                    
                    break;
            }
        }
    }
}
