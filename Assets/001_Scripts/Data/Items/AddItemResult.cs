using System.Collections.Generic;

namespace AstraNope.Data.Items
{
    public readonly struct AddItemResult
    {
        public readonly int remain;
        public readonly List<int> changeKeys;

        public AddItemResult(int remain, List<int> changeKeys)
        {
            this.remain = remain;
            this.changeKeys = changeKeys;
        }
    }
}