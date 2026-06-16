using System.Collections.Generic;
using UnityEngine;

namespace _001_Scripts.Data.SOJ
{
    [CreateAssetMenu(fileName = "BluePrints", menuName = "Data/Create BluePrints", order = 0)]
    public class BluePrintDataBase : ScriptableObject
    {
        public List<BluePrint.BluePrint> bluePrints = new();
        public BluePrint.BluePrint GetBluePrint(int id) {
            BluePrint.BluePrint obj = bluePrints.Find(item =>
                item.bluePrintId == id);
            return obj;
        }
        public BluePrint.BluePrint GetBluePrint(string name)
        {
            BluePrint.BluePrint obj = bluePrints.Find(item =>
                item.bluePrintName == name);
            return obj;
        }
        public BluePrint.BluePrint GetBluePrint(BluePrint.BluePrint bluePrint)
        {
            BluePrint.BluePrint obj = bluePrints.Find(i => i == bluePrint);
            return obj;
        }
        
        /// <summary>
        /// Made Only For Read
        /// Don't do change the BluePrint desc or info.
        /// It can be broken by the BluePrint DB System.
        /// </summary>
        /// <returns>All of Blueprints</returns>
        public IReadOnlyList<BluePrint.BluePrint> GetAllBluePrints() => bluePrints;
        public bool Exist(int id) => bluePrints.Exists(item => item.bluePrintId == id);
    }
}