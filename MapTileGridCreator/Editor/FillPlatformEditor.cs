using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MapTileGridCreator.Core;
using MapTileGridCreator.Utilities;

[CustomEditor(typeof(FillPlatform))]

public class FillPlatformEditor : Editor
{
  // Start is called before the first frame update
  public override void OnInspectorGUI()
  {
    base.OnInspectorGUI();
    if (GUILayout.Button( "Generate"))
    {
      FillPlatform pf = target as FillPlatform;

      for (int i = (int)pf.start.x; i < (int)pf.end.x; i += (int)pf.size.x)
      {
        for (int j = (int)pf.start.z; j < (int)pf.end.z; j += (int)pf.size.z)
        {

          Vector3 pointWorld = new Vector3(pf.start.x + i, pf.offy, pf.start.z + j);

          GameObject go = PrefabUtility.InstantiatePrefab(pf.prefab, pf._grid.gridRoot.transform) as GameObject;
          Vector3Int index = new Vector3Int((i - (int)pf.start.x) / (int)pf.size.x, 0, (j - (int)pf.start.z) / (int)pf.size.z);
          Cell cell = go.GetComponent<Cell>();
          if (cell == null)
          {
            cell = go.AddComponent<Cell>();
          }
          pf._grid.AddCell(index, cell);
          cell.ResetTransform();
        }

      }

    }
  }
}
