using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public List<GameObject> m_Prefabs;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        while(m_Prefabs.Count > 0)
        {
            yield return new WaitForSeconds(1.0f);

            GameObject prefab = m_Prefabs[0];

            Instantiate(prefab);

            m_Prefabs.RemoveAt(0);
        }
    }
}
