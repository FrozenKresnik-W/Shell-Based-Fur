using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Aperture.Fur.Runtime
{
    public class FurOverdrawTest : MonoBehaviour
    {
        public float maxOverdraw = 5;

        // Update is called once per frame
        void Update()
        {
            FurLayerBalancer.SetMaxOverdraw(maxOverdraw);
        }
    }
}
