using UnityEngine;

namespace BitDivisible.Samples
{
    [BitDivisible]
    partial class Data
    {
        [BitDivisibleField(typeof(int), 3, "A")]
        [BitDivisibleField(typeof(bool), 1, "B")]
        public ulong i;
    }

    class BitDivisibleTest : MonoBehaviour
    {
        readonly ulong[] ary = { 1, 2, 4, 8, 16, };

        [ContextMenu("Test")]
        void Start()
        {
            var data = new Data();
            foreach (var i in ary)
            {
                data.i = i;
                Debug.Log($"A:{data.A}, B:{data.B}");
            }

            data.SetI(1, true);
            Debug.LogWarning(data.i);
        }
    }
}
