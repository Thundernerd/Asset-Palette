using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyTheunissen.AssetPalette
{
    [Serializable]
    public class PaletteFolder
    {
        [SerializeField] private string name;
        public string Name => name;

        [SerializeReference] private List<PaletteFolder> children = new List<PaletteFolder>();
        public List<PaletteFolder> Children => children;
        
        [SerializeReference] private List<PaletteEntry> entries = new List<PaletteEntry>();
        public List<PaletteEntry> Entries => entries;

        [SerializeField] private string guid;
        
        public void Initialize(string name)
        {
            this.name = name;
            guid = Guid.NewGuid().ToString();
        }

        public override string ToString()
        {
            return $"{GetType().Name}({Name}, {guid})";
        }
    }
}
