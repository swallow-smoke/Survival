using System.Collections.Generic;
using _001_Scripts.Obj;
using UnityEngine;

namespace _001_Scripts.Controller
{
    public class InventoryController : MonoBehaviour
    {
        public List<Item> inventory = new List<Item>();

        public void Sort(bool isReverse)
        {
            inventory.Sort();
        }
    }
}