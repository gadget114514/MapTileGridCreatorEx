using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MapTileGridCreator.Core;
using MapTileGridCreator.CubeImplementation;
using MapTileGridCreator.HexagonalImplementation;
using UnityEditor;

[CustomEditor(typeof(GridPlacePrefab))]
public class GridPlacePrefabEditor : Editor
{
	// Start is called before the first frame update
	public override void OnInspectorGUI()
	{
		GridPlacePrefab p = target as GridPlacePrefab;
		
		int countx = p.countx, countz = p.countz;
		int gridx = p.gridx, gridz = p.gridz;
		int height = p.height;
		int maxcount = p.maxcount;
		
		base.OnInspectorGUI();
		
		if (GUILayout.Button("Gen")) 
		{
			int count = 0;
			for (int x = 0; x < countx; x++) {
				for (int z = 0; z < countz; z++) {
					
					float r = Random.Range(0f,1f);
					if (r > p.probability) continue;
					
					if (count < maxcount) {
			GameObject go = PrefabUtility.InstantiatePrefab(p.prefab) as GameObject;
			
						go.transform.position = new Vector3(gridx/2+x*gridx,height,gridz/2+z*gridz);
						go.transform.parent = p.manager.transform;
						count++;
					}
			
				}
			}
			
		}
		
	}
}
