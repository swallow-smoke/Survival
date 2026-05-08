using System.Collections.Generic;
using _001_Scripts.Obj;
using UnityEngine;

namespace _001_Scripts.Data.SOJ
{
    [CreateAssetMenu(fileName = "ItemDataBase", menuName = "Data/Create ItemDB", order = 0)]
    public class ItemDataBase : ScriptableObject
    {
        public List<Item> _materials;
        public List<Item> _weapons;
        public List<Item> _armors;
    }
}