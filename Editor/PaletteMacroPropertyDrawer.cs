using RoyTheunissen.AssetPalette.Runtime;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AssetPalette.Editor
{
    /// <summary>
    /// Draws an Asset entry in the palette.
    /// </summary>
    [CustomPropertyDrawer(typeof(PaletteMacro))]
    public class PaletteMacroPropertyDrawer : PaletteEntryPropertyDrawer<PaletteMacro>
    {
        private Texture2D cachedMacroIcon;
        private bool didCacheShortcutIcon;
        private Texture2D MacroIcon
        {
            get
            {
                if (!didCacheShortcutIcon)
                {
                    didCacheShortcutIcon = true;
                    cachedMacroIcon = Resources.Load<Texture2D>("Palette Macro Icon");
                }
                return cachedMacroIcon;
            }
        }

        protected override string OpenText => "Run";

        protected override void DrawContents(Rect position, SerializedProperty property, PaletteMacro entry)
        {
            // OPTIMIZATION: Don't bother with any of this if we're not currently drawing.
            if (Event.current.type != EventType.Repaint)
                return;

            // If we don't have a nice rendered preview, draw an icon instead.
            float width = Mathf.Min(MacroIcon.width, position.width * 0.75f);
            Vector2 size = new Vector2(width, width);
            Rect iconRect = new Rect(position.center - size / 2, size);
                
            GUI.DrawTexture(iconRect, MacroIcon, ScaleMode.ScaleToFit);
        }

        protected override void OnContextMenu(GenericMenu menu, PaletteMacro entry)
        {
            base.OnContextMenu(menu, entry);
            
            menu.AddItem(new GUIContent("Edit Script"), false, EditScript, entry);
            menu.AddItem(new GUIContent("Show Script In Project Window"), false, ShowInProjectWindow, entry);
            menu.AddItem(new GUIContent("Show Script In Explorer"), false, ShowInExplorer, entry);
        }

        private void EditScript(object userData)
        {
            PaletteMacro entry = (PaletteMacro)userData;
            
            AssetDatabase.OpenAsset(entry.Script);
        }

        private void ShowInProjectWindow(object userData)
        {
            PaletteMacro entry = (PaletteMacro)userData;
            
            EditorGUIUtility.PingObject(entry.Script);
        }

        private void ShowInExplorer(object userData)
        {
            PaletteMacro entry = (PaletteMacro)userData;

            ShowAssetInExplorer(entry.Script);
        }
    }
}
