﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

using MapTileGridCreator.Core;
using MapTileGridCreator.SerializeSystem;
using MapTileGridCreator.Utilities;

using UnityEditor;

using UnityEngine;

/// <summary>
/// Main window class.
/// </summary>
[CanEditMultipleObjects]
public class MapTileGridCreatorWindow : EditorWindow
{
	private enum EditMode
	{
		Select, Paint, Procedural
	};

	private enum SelectMode
	{
		Default, Move, Stamp
	};

	private enum PaintMode
	{
		Single, Erase, Eyedropper
	};

	//Global
	private string[] _modes_string = new string[] { "Select", "Paint", "Procedural" };
	private EditMode _mode_edit;

	private Grid3D _grid;
	[SerializeField]
	private bool _debug_grid = true;
	[SerializeField]
	private int _size_grid = 20;
	[SerializeField]
	private int _offset_grid_y = 0;
	[SerializeField]
	private Quaternion rotation;

	private Vector2 _scroll_position;

	//Creation empty
	private TypeGrid3D _empty_creation_choice;

	//Selection
	private List<Cell> _selection = new List<Cell>();
	private GUIContent[] _modes_select;
	private SelectMode _mode_select;
	private bool _overwrite_cells_modif;

	//Paint
	private GUIContent[] _modes_paint;
	private PaintMode _mode_paint;

	private Plane _plane_y = new Plane();
	[SerializeField]
	[Min(1)]
	private float _dist_default_interaction = 100.0f;
	[SerializeField]
	private bool _collide_with_plane = true;
	[SerializeField]
	private string _path_pallet = "Assets/MapTileGridCreatorEx/MapTileGridCreator/Pallets/Cubes";
	private int _pallet_index;
	private List<GameObject> _pallet = new List<GameObject>();

	//Procedural
	private MapModifier _map_gen;
	private bool _edit_transformations;
	[SerializeField]
	private bool _debug_start_modifiers;

	//	[MenuItem("MapTileGridCreator/Open")]
	public static void OpenWindows(Grid3D _grid)
	{
		MapTileGridCreatorWindow window = (MapTileGridCreatorWindow)GetWindow(typeof(MapTileGridCreatorWindow));
		if (_grid != null) window.UpdateGridSelected(_grid);
		window.Show();
	}

	private void OnEnable()
	{
		_modes_paint = new GUIContent[] {
			new GUIContent(EditorGUIUtility.IconContent("Grid.PaintTool", "Paint one by one the prefab selected")),
			new GUIContent(EditorGUIUtility.IconContent("Grid.EraserTool", "Erase the cell in scene view")),
			new GUIContent(EditorGUIUtility.IconContent("Grid.PickingTool", "Eyedropper to auto select the corresponding prefab in pallete")) };

		_modes_select = new GUIContent[] {
			new GUIContent(EditorGUIUtility.IconContent("Grid.Default", "Default, select or or multiple cells in scene view")),
			new GUIContent(EditorGUIUtility.IconContent("Grid.MoveTool", "Move selected cells to a given destination")),
			new GUIContent(EditorGUIUtility.IconContent("TreeEditor.Duplicate", "Copy selected and paste to a given destination")) };
	}

	private void OnFocus()
	{
		/* 2018.4.22f1
		SceneView.onSceneGUIDelegate -= OnSceneGUI;
		SceneView.onSceneGUIDelegate += OnSceneGUI;
		 */
		SceneView.duringSceneGui -= OnSceneGUI;
		SceneView.duringSceneGui += OnSceneGUI;
		if (_grid != null)
		{
			FuncEditor.RefreshGrid(_grid);
		}
		RefreshPallet();
	}

	private void RefreshPallet()
	{
		_pallet.Clear();

		Debug.Log("Path=" + _path_pallet);

		string[] prefabFiles = Directory.GetFiles(_path_pallet, "*.prefab");
		if (prefabFiles == null) return;
		Debug.Log("Prefabs=" + prefabFiles.Length);
		foreach (string prefabFile in prefabFiles)
		{
			//	Debug.Log("prefabs="+prefabFile);
			_pallet.Add(AssetDatabase.LoadAssetAtPath(prefabFile, typeof(GameObject)) as GameObject);
		}
	}

	#region SceneManagement

	
	private void UpdateDebugScene(Grid3D _grid) {
		//Update grid debug position
		Vector3 positionGrid = _grid.Origin;
		positionGrid.y += _offset_grid_y;
		_plane_y.SetNormalAndPosition(_grid.GetAxe(1), positionGrid);

		SwitchEditMode();

		if (_debug_grid)
		{
			FuncEditor.DebugGrid(_grid, DebugsColor.grid_help, _offset_grid_y, _size_grid);
		}
	}

	private void OnSceneGUI(SceneView sceneView)
	{
		SelectionGridSceneView();
		OnSelectionEditChangedCheck();

		if (_grid != null)
		{
			//Update grid debug position
			Vector3 positionGrid = _grid.Origin;
			positionGrid.y += _offset_grid_y;
			_plane_y.SetNormalAndPosition(_grid.GetAxe(1), positionGrid);

			SwitchEditMode();

			if (_debug_grid)
			{
				FuncEditor.DebugGrid(_grid, DebugsColor.grid_help, _offset_grid_y, _size_grid);
			}
		}
	}

	/// <summary>
	/// Test selection for auto link grid to the window.
	/// </summary>
	public void SelectionGridSceneView()
	{
		GameObject select = Selection.activeGameObject;
		if (select != null)
		{
			Grid3D grid = null;
			if (FuncEditor.IsGameObjectSceneView(select))
			{
				grid = select.GetComponent<Grid3D>();
				if (grid == null)
				{
					Cell cell = select.GetComponent<Cell>();
					grid = cell != null ? cell.GetGridParent() : null;
				}
			}

			if (grid != null)
			{
				UpdateGridSelected(grid);
			}
		}
	}

	/// <summary>
	/// Update the working grid.
	/// </summary>
	/// <param name="selectedGrid"></param>
	private void UpdateGridSelected(Grid3D selectedGrid)
	{
		//	Debug.Log("UpdateGridSelected " + selectedGrid);
		
		if (selectedGrid != null)
		{
			if (FuncEditor.IsGameObjectInstancePrefab(selectedGrid.gameObject))
			{
				if (EditorUtility.DisplayDialog("Modify Existing Grid Prefab", "The grid selected is a prefab and cannot be modified unless you unpack it. " +
				"\n Do you want to continue ?", "Yes", "No"))
				{
					PrefabUtility.UnpackPrefabInstance(selectedGrid.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
				}
				else
				{
					selectedGrid = null;
				}
			}
		}
		else
		{
			_grid = null;
		}

		if (selectedGrid != null && selectedGrid != _grid)
		{
			_grid = selectedGrid;
			if (_path_pallet != _grid.palletFolderPath) {
		    	_path_pallet = _grid.palletFolderPath;
				RefreshPallet();
				UpdateDebugScene(_grid);
			}
		}
	}

	/// <summary>
	/// Funtion to add SelectInput to delegate OnSelectionChanged when mode selection activated.
	/// </summary>
	private void OnSelectionEditChangedCheck()
	{
		if (_mode_edit == EditMode.Select && _mode_select == SelectMode.Default)
		{
			Selection.selectionChanged += SelectInput;
		}
		else
		{
			Selection.selectionChanged -= SelectInput;
		}
	}

	private void SelectInput()
	{
		Cell[] selection = Selection.GetFiltered<Cell>(SelectionMode.TopLevel);
		ReplaceSelection(selection);
	}

	private void ReplaceSelection(Cell[] selection)
	{
		_selection.Clear();
		foreach (Cell c in selection)
		{
			_selection.Add(c);
		}
		_selection = _selection.OrderBy(x => x.transform.position.y).ToList();
	}

	private void SwitchEditMode()
	{
		switch (_mode_edit)
		{
			case EditMode.Select:
				SelectEdit();
				break;
			case EditMode.Paint:
				PaintEdit();
				break;
			case EditMode.Procedural:
				if (_debug_start_modifiers)
				{
					int i = 0;
					foreach (Modifier m in _map_gen.m_ModifiersList)
					{
						if (m != null && !m.Pass)
						{
							Vector3Int startIdx = m.StartIndex;
							Vector3 posTrans = _grid.GetPositionCell(ref startIdx );
							DebugCellAtPosition(posTrans, DebugsColor.start_modifier, 1.1f);
							posTrans.y += _grid.SizeCell.y;
							Handles.Label(posTrans, "Start modifier " + i);
						}
						i++;
					}
				}
				break;
		}
	}

	private void SelectEdit()
	{
		switch (_mode_select)
		{
			case SelectMode.Move:
				MoveInput();
				break;

			case SelectMode.Stamp:
				StampInput();
				break;
		}
	}

	////////////////////////////////////////
	// Select differents modes

	private void MoveInput()
	{
		if (Event.current.type == EventType.Layout)
		{
			HandleUtility.AddDefaultControl(0);
		}

		if (_selection.Count != 0)
		{
			//Preview destination
			Vector3 input = GetGridPositionInput(0.5f, _collide_with_plane);
			Vector3Int destination = _grid.GetIndexByPosition(ref input);
			DebugSelection(destination);

			//Move apply
			if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
			{
				Undo.SetCurrentGroupName("Move selected");
				int group = Undo.GetCurrentGroup();

				FuncEditor.StampCells(_selection, _grid, destination, _overwrite_cells_modif);
				foreach (Cell c in _selection)
				{
					Undo.DestroyObjectImmediate(c.gameObject);
				}

				Undo.CollapseUndoOperations(group);
				_mode_select = SelectMode.Default;
				Selection.SetActiveObjectWithContext(_grid.gameObject, null);
			}
			//Reset selection
			else if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
			{
				_mode_select = SelectMode.Default;
			}
		}
	}

	private void StampInput()
	{
		if (Event.current.type == EventType.Layout)
		{
			HandleUtility.AddDefaultControl(0);
		}

		if (_selection.Count != 0)
		{
			//Preview destination
			Vector3 input = GetGridPositionInput(0.5f, _collide_with_plane);
			Vector3Int destination = _grid.GetIndexByPosition(ref input);
			DebugSelection(destination);

			//Apply stamp
			if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
			{
				Undo.SetCurrentGroupName("Stamp selected");
				int group = Undo.GetCurrentGroup();
				FuncEditor.StampCells(_selection, _grid, destination, _overwrite_cells_modif);
				Undo.CollapseUndoOperations(group);
			}
			//Reset selection
			else if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
			{
				_mode_select = SelectMode.Default;
			}
		}
	}

	private void DebugSelection(Vector3Int destinationIndex)
	{
		Vector3Int displacement = destinationIndex - _selection[0].GetIndex();
		foreach (Cell c in _selection)
		{
			Vector3Int index = c.GetIndex() + displacement;
			Vector3 destination = _grid.GetPositionCell(ref index);
			DebugCellAtPosition(destination, DebugsColor.destination_editor);
		}
	}

	////////////////////////////////////////
	// Paint differents modes

	private void PaintEdit()
	{
		switch (_mode_paint)
		{
			case PaintMode.Single:
				{
					Vector3 input = GetGridPositionInput(0.5f, _collide_with_plane);
					PaintInput(input);
					DebugCellAtPosition(input, DebugsColor.destination_editor);
				}
				break;

			case PaintMode.Erase:
				{
					Vector3 input = GetGridPositionInput(-0.5f, false);
					EraseInput(input);
					Cell selected = _grid.TryGetCellByPosition(ref input);
					if (selected != null)
					{
						DebugCellAtPosition(input, DebugsColor.erase_editor, 1.5f);
					}
				}
				break;

			case PaintMode.Eyedropper:
				{
					Vector3 input = GetGridPositionInput(-0.1f, false);
					Cell selected = _grid.TryGetCellByPosition(ref input);
					EyedropperInput(selected);
					if (selected != null)
					{
						DebugCellAtPosition(selected.transform.position, DebugsColor.eyedropper_editor, 1.05f);
					}
				}
				break;
		}
	}

	private void PaintInput(Vector3 pointWorld)
	{
	

		if (Event.current.type == EventType.Layout)
		{
			HandleUtility.AddDefaultControl(0);
		}

		//Apply paint
		if (_pallet_index < _pallet.Count && Event.current.type == EventType.MouseDown && Event.current.button == 0)
		{
			Debug.Log("PaintInput=" + pointWorld);

			GameObject prefab = _pallet[_pallet_index];
			FuncEditor.InstantiateCell(prefab, _grid, pointWorld, rotation);
		}
	}

	private void EraseInput(Vector3 input)
	{
		if (Event.current.type == EventType.Layout)
		{
			HandleUtility.AddDefaultControl(0);
		}

		//Erase cell
		if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && Event.current.button == 0)
		{
			Vector3Int index = _grid.GetIndexByPosition(ref input);
			Cell cell = _grid.TryGetCellByIndex(ref index);

			if (cell != null)
			{
				_grid.DeleteCell(cell);
				Selection.SetActiveObjectWithContext(_grid.gameObject, null);
				Undo.DestroyObjectImmediate(cell.gameObject);
			}
		}
	}

	private void EyedropperInput(Cell selectedCell)
	{
		if (Event.current.type == EventType.Layout)
		{
			HandleUtility.AddDefaultControl(0); // Consume the event
		}

		//Select prefab from pallett and got to paint
		if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
		{
			GameObject prefabInstance = selectedCell.gameObject;
			GameObject prefab = FuncEditor.GetPrefabFromInstance(prefabInstance);

			int newIndex = _pallet.FindIndex(x => x.Equals(prefab));
			if (newIndex >= 0)
			{
				_mode_paint = (int)PaintMode.Single;
				_pallet_index = newIndex;
				Debug.Log("Prefab " + prefab.name + " selected.");
			}
			else
			{
				Debug.LogError("Prefab is not from the pallet");
			}
		}
		//Cancel
		else if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
		{
			_mode_paint = PaintMode.Single;
		}
	}


	////////////////////////////////////////
	// Utilities scene view

	private Vector3 GetGridPositionInput(float offset_normal_factor, bool canCollideWithPlane)
	{
		Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
		Vector3 hitPoint = Vector3.zero;
		float enter = 0.0f;
		bool isPlaneCollided = canCollideWithPlane && _plane_y.Raycast(ray, out enter);
		if (Physics.Raycast(ray, out RaycastHit hit, _dist_default_interaction * (_grid.SizeCell.y + 1)) && (!isPlaneCollided || (isPlaneCollided && hit.distance < enter)))
		{
			Debug.Log("Raycast 1");

			hitPoint = hit.point;
			if (hit.collider.gameObject.GetComponent<Cell>() != null || hit.collider.GetComponentInParent<Cell>())
			{
				hitPoint = hitPoint + V3M.mult(hit.normal, _grid.SizeCell) * offset_normal_factor;
			}
		}
		else if (isPlaneCollided && enter < _dist_default_interaction * _grid.SizeCell.y)
		{
			Debug.Log("Raycast 2");

			hitPoint = ray.GetPoint(enter);
		}
		else
		{
			Debug.Log("Raycast 3");
			Camera cam = SceneView.currentDrawingSceneView.camera;
		//	Plane p = new Plane(Vector3.up, Vector3.zero);
	//		cam.transform.position, ray.
			//	hitPoint = ray.GetPoint(_dist_default_interaction * _grid.SizeCell.y);
			if (_plane_y.Raycast(ray, out enter)) {
				hitPoint = ray.GetPoint(enter);
	//			Debug.Log("3 = " + enter + " " + hitPoint);
			}
		}

		return hitPoint;
	}

	private void DebugCellAtPosition(Vector3 position, Color color, float size_factor = 1f)
	{
		Debug.Log("Cell At Position " + position + " " + color);
		using (new Handles.DrawingScope(color))
		{
			Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
			Vector3 pos = _grid.TransformPositionToGridPosition(position);
			Handles.color = color;
			Debug.Log("pos = " + pos);
			Handles.DrawWireCube(pos, (_grid.SizeCell * size_factor));
		}
	}

	#endregion

	////////////////////////////////////////

	#region MenusManagement

	private void OnGUI()
	{
		DrawMainMenu();
	}

	private void DrawMainMenu()
	{
		GUILayout.Label("Map Tile Grid Creator Settings", EditorStyles.boldLabel);

		DrawNewGridPanel();
		FuncEditor.DrawUILine(Color.gray);

		EditorGUILayout.LabelField("Map selected :", EditorStyles.boldLabel);
		Grid3D newgrid = (Grid3D)EditorGUILayout.ObjectField(_grid, typeof(Grid3D), true);
		UpdateGridSelected(newgrid);

		if (_grid != null)
		{
			EditorGUILayout.LabelField("Number of cells : " + _grid.GetNumberOfCells());
		}

		GUILayout.BeginHorizontal();

		if (GUILayout.Button("Load"))
		{
			string fullpath = EditorUtility.OpenFilePanel("File map load", "", "json");
			if (fullpath != "")
			{
				_grid = SaveLoadFileSystem.LoadRawJSON(fullpath);
			}

			Selection.SetActiveObjectWithContext(_grid, null);
		}

		if (_grid != null)
		{
			if (GUILayout.Button("Save"))
			{
				string fullpath = EditorUtility.SaveFilePanel("File map save", "", _grid.name, "json");
				if (fullpath != "")
				{
					SaveLoadFileSystem.SaveAsyncRawJSON(_grid, fullpath);
				}
			}
		}
		GUILayout.EndHorizontal();
		if (_grid != null)
		{
			if (GUILayout.Button("Check"))
			{
				var cells = _grid.GetComponentsInChildren<Cell>();
				Dictionary<Vector3Int, Cell> dic = new Dictionary<Vector3Int, Cell>();
				for (int i = 0; i < cells.Length; i++)
				{
					if (dic.ContainsKey(cells[i].GetIndex()))
					{
						Debug.Log("Check error at " + cells[i].GetIndex() + " " + dic[cells[i].GetIndex()].gameObject  + dic[cells[i].GetIndex()].gameObject.GetInstanceID() + " and " + cells[i].gameObject.name + cells[i].gameObject.GetInstanceID());
					} else 
					dic[cells[i].GetIndex()] = cells[i];
				}
			}
			if (GUILayout.Button("Clear Dup"))
			{
				var cells = _grid.GetComponentsInChildren<Cell>();
				Dictionary<Vector3Int, Cell> dic = new Dictionary<Vector3Int, Cell>();
				for (int i = 0; i < cells.Length; i++)
				{
					if (dic.ContainsKey(cells[i].GetIndex()))
					{
						Debug.Log("Check error at " + cells[i].GetIndex() + " " + dic[cells[i].GetIndex()].gameObject + dic[cells[i].GetIndex()].gameObject.GetInstanceID() + " and " + cells[i].gameObject.name + cells[i].gameObject.GetInstanceID());
						if (dic[cells[i].GetIndex()].gameObject.transform.position == Vector3.zero)
						{
							if (cells[i].GetIndex() != Vector3Int.zero)
							{
								DestroyImmediate(dic[cells[i].GetIndex()].gameObject);
							}
						}
						if (cells[i].gameObject.transform.position == Vector3.zero)
						{
							if (cells[i].GetIndex() != Vector3Int.zero)
							{
								DestroyImmediate(cells[i].gameObject);
							}
						}
					}
					else
					{
						dic[cells[i].GetIndex()] = cells[i];
					}
				}
			}
		}


		if (_grid != null)
		{
			_debug_grid = EditorGUILayout.Toggle("Debug grid", _debug_grid);
			if (_debug_grid)
			{
				_size_grid = EditorGUILayout.IntField("Size grid", _size_grid);
				_offset_grid_y = EditorGUILayout.IntField("Offset Y Ground Plane", _offset_grid_y);
			}
				rotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", rotation.eulerAngles));
			Vector3 v = rotation.eulerAngles;
			if (GUILayout.Button("Rotation")) rotation = Quaternion.Euler(new Vector3(v.x, Mathf.RoundToInt(v.y + 90) >= 360 ? 0 : (v.y+90), v.z)) ;

			FuncEditor.DrawUILine(Color.gray);
			EditorGUILayout.LabelField("Tools :", EditorStyles.boldLabel);
			_dist_default_interaction = EditorGUILayout.Slider("Distance default interaction : ", _dist_default_interaction, 0.0f, 500.0f);
			DrawEditor();
		}
	}

	private void DrawNewGridPanel()
	{
		FuncEditor.DrawUILine(Color.gray);
		GUILayout.Label("Create empty", EditorStyles.boldLabel);
		_empty_creation_choice = (TypeGrid3D)EditorGUILayout.Popup((int)_empty_creation_choice, _empty_creation_choice.GetTypesGrid());
		if (GUILayout.Button("New"))
		{
			_grid = FuncEditor.InstantiateGrid3D(_empty_creation_choice);
			Selection.SetActiveObjectWithContext(_grid.gameObject, null);
		}
	}

	private void DrawEditor()
	{
		//Select, move, paint, erase, fill, paint fill, delete, pipette
		_mode_edit = (EditMode)GUILayout.Toolbar((int)_mode_edit, _modes_string);

		FuncEditor.DrawUILine(Color.gray);
		switch (_mode_edit)
		{
			case EditMode.Select:
				DrawPanelSelect();
				break;
			case EditMode.Paint:
				DrawBrushPanel();
				FuncEditor.DrawUILine(Color.gray);
				DrawPanelPallet();
				break;
			case EditMode.Procedural:
				DrawMapModifierPanel();
				break;

		}
	}

	private void DrawPanelSelect()
	{
		EditorGUILayout.LabelField("Select modes :");

		_mode_select = (SelectMode)GUILayout.Toolbar((int)_mode_select, _modes_select);
		switch (_mode_select)
		{
			case SelectMode.Default:
				if (GUILayout.Button("Replace selected") && _pallet_index < _pallet.Count)
				{
					Undo.SetCurrentGroupName("Replace selected");
					int group = Undo.GetCurrentGroup();

					GameObject prefab = _pallet[_pallet_index];
					foreach (Cell c in _selection)
					{
						FuncEditor.ReplaceCell(prefab, _grid, c);
					}
					Undo.CollapseUndoOperations(group);

					Selection.SetActiveObjectWithContext(_grid.gameObject, null);
				}
				if (GUILayout.Button("Rotate selected"))
				{
					Undo.SetCurrentGroupName("Rotate selected");
					int group = Undo.GetCurrentGroup();

					foreach (Cell c in _selection)
					{
						float currentRotation = c.gameObject.transform.localRotation.eulerAngles.y;
     // get desired rotation
     float yRotation =  Mathf.Round((currentRotation + 90)/ 90) * 90f;
     // set rotation
						c.gameObject.transform.localRotation = Quaternion.Euler(c.gameObject.transform.rotation.x, yRotation, c.gameObject.transform.rotation.z);
					}
					
					Undo.CollapseUndoOperations(group);
				}
				if (GUILayout.Button("Move x"))
				{
					foreach (Cell c in _selection) FuncEditor.MoveCell(_grid, c, new Vector3(1,0,0));
				}
				if (GUILayout.Button("Move -x"))
				{
					foreach (Cell c in _selection) FuncEditor.MoveCell(_grid, c, new Vector3(-1,0,0));
				}
				if (GUILayout.Button("Move z"))
				{
					foreach (Cell c in _selection) FuncEditor.MoveCell(_grid, c, new Vector3(0,0,1));
				}
				if (GUILayout.Button("Move -z"))
				{
					foreach (Cell c in _selection) FuncEditor.MoveCell(_grid, c, new Vector3(0,0,-1));
				}
				if (GUILayout.Button("Move y"))
				{
					foreach (Cell c in _selection) FuncEditor.MoveCell(_grid, c, new Vector3(0,1,0));
				}
				if (GUILayout.Button("Move -y"))
				{
					foreach (Cell c in _selection) FuncEditor.MoveCell(_grid, c, new Vector3(0,-1,0));
				}
				FuncEditor.DrawUILine(Color.gray);
				DrawPanelPallet();
				break;

			case SelectMode.Move:
				if (_selection.Count == 0)
				{
					EditorGUILayout.HelpBox("Need cells selected to be effective.", MessageType.Warning);
				}
				if (GUILayout.Button("Rotate selected"))
				{
					Undo.SetCurrentGroupName("Rotate selected");
					int group = Undo.GetCurrentGroup();

					foreach (Cell c in _selection)
					{
						float currentRotation = c.gameObject.transform.localRotation.eulerAngles.y;
						// get desired rotation
						float yRotation =  Mathf.Round((currentRotation + 90)/ 90) * 90f;
						// set rotation
						c.gameObject.transform.localRotation = Quaternion.Euler(c.gameObject.transform.rotation.x, yRotation, c.gameObject.transform.rotation.z);
					}
					
					Undo.CollapseUndoOperations(group);
				}
				if (GUILayout.Button("Move x"))
				{
                    foreach (Cell c in _selection) FuncEditor.MoveCell(_grid, c, new Vector3(1,0,0));
				}
				if (GUILayout.Button("Move -x"))
				{
					foreach (Cell c in _selection) FuncEditor.MoveCell(_grid, c, new Vector3(-1,0,0));
				}
				if (GUILayout.Button("Move z"))
				{
					foreach (Cell c in _selection) FuncEditor.MoveCell(_grid, c, new Vector3(0,0,1));
				}
				if (GUILayout.Button("Move -z"))
				{
					foreach (Cell c in _selection) FuncEditor.MoveCell(_grid, c, new Vector3(0,0,-1));
				}
				if (GUILayout.Button("Move y"))
				{
					foreach (Cell c in _selection) FuncEditor.MoveCell(_grid, c, new Vector3(0,1,0));
				}
				if (GUILayout.Button("Move -y"))
				{
					foreach (Cell c in _selection) FuncEditor.MoveCell(_grid, c, new Vector3(0,-1,0));
				}
				
				_collide_with_plane = EditorGUILayout.Toggle("Collide with ground plane : ", _collide_with_plane);
				_overwrite_cells_modif = EditorGUILayout.Toggle("Overwrite existing cells", _overwrite_cells_modif);
				break;

			case SelectMode.Stamp:
				if (_selection.Count == 0)
				{
					EditorGUILayout.HelpBox("Need cells selected to be effective.", MessageType.Warning);
				}

				_collide_with_plane = EditorGUILayout.Toggle("Collide with ground plane : ", _collide_with_plane);
				_overwrite_cells_modif = EditorGUILayout.Toggle("Overwrite existing cells", _overwrite_cells_modif);
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Load from prefab"))
				{
					string fullpath = EditorUtility.OpenFilePanel("File prefab stamp load", "", "prefab");
					if (fullpath != "")
					{
						GameObject prefab = PrefabUtility.LoadPrefabContents(fullpath);
						ReplaceSelection(prefab.GetComponentsInChildren<Cell>());
					}

					Selection.SetActiveObjectWithContext(_grid, null);
				}

				if (_selection.Count != 0 && GUILayout.Button("Save to prefab"))
				{
					string fullpath = EditorUtility.SaveFilePanel("File prefab stamp save", "", "", "prefab");
					if (fullpath != "")
					{
						string relative = fullpath.Remove(0, Directory.GetCurrentDirectory().Length + 1);
						GameObject prefab = new GameObject();
						foreach (Cell c in _selection)
						{
							GameObject prefabChild = FuncEditor.GetPrefabFromInstance(c.gameObject);
							GameObject child = PrefabUtility.InstantiatePrefab(prefabChild, prefab.transform) as GameObject;
							child.name = c.name;
							child.transform.position = c.transform.position;
							UnityEditorInternal.ComponentUtility.CopyComponent(c);
							UnityEditorInternal.ComponentUtility.PasteComponentValues(child.GetComponent<Cell>());
						}
						PrefabUtility.SaveAsPrefabAsset(prefab, relative);
						DestroyImmediate(prefab);
					}
				}
				GUILayout.EndHorizontal();

				break;
		}
	}

	private void DrawBrushPanel()
	{
		EditorGUILayout.LabelField("Brush panel :");
		_mode_paint = (PaintMode)GUILayout.Toolbar((int)_mode_paint, _modes_paint);
		switch (_mode_paint)
		{
			case PaintMode.Single:
				{
					_collide_with_plane = EditorGUILayout.Toggle("Collide with ground plane : ", _collide_with_plane);
				}
				break;

			case PaintMode.Erase:
				{ }
				break;

			case PaintMode.Eyedropper:
				{ }
				break;
		}
	}

	private void DrawPanelPallet()
	{
		EditorGUILayout.LabelField("Pallet panel :");
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Go root folder"))
		{
			UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_path_pallet);
			Selection.SetActiveObjectWithContext(folder, null);
		}

		if (GUILayout.Button("Open new pallet"))
		{
			string fullpath = EditorUtility.OpenFolderPanel("Load pallet", _path_pallet, "");
			string relative = fullpath.Remove(0, Directory.GetCurrentDirectory().Length + 1);
			if (relative != "")
			{
				_path_pallet = relative;
				RefreshPallet();
			}
		}

		if (GUILayout.Button("Clone"))
		{
			DirectoryInfo dir = new DirectoryInfo(_path_pallet);

			string fullpath = EditorUtility.OpenFolderPanel("New pallet cloned", _path_pallet, "");
			string clone = fullpath.Remove(0, Directory.GetCurrentDirectory().Length + 1);

			if (!Directory.Exists(clone))
			{
				Directory.CreateDirectory(clone);
			}

			FileInfo[] prefabFiles = dir.GetFiles("*.prefab");
			foreach (FileInfo file in prefabFiles)
			{
				string temppath = Path.Combine(clone, file.Name);
				file.CopyTo(temppath, false);
			}
			_path_pallet = clone;
			RefreshPallet();
		}

		GUILayout.EndHorizontal();
		GUILayout.Label("Actual folder : " + _path_pallet);

		if (_pallet.Count == 0)
		{
			EditorGUILayout.HelpBox("No prefab founded for pallet.", MessageType.Warning);
		}
		else
		{
			List<GUIContent> palletIcons = new List<GUIContent>();
			foreach (GameObject prefab in _pallet)
			{
				Texture2D texture = AssetPreview.GetAssetPreview(prefab);
				GUIContent preview = new GUIContent(texture, prefab.name);
				palletIcons.Add(preview);
			}
			_scroll_position = GUILayout.BeginScrollView(_scroll_position);
			_pallet_index = GUILayout.SelectionGrid(_pallet_index, palletIcons.ToArray(), 2);
			GUILayout.EndScrollView();
		}
	}

	private void DrawMapModifierPanel()
	{
		EditorGUILayout.LabelField("Map modifier :");
		_map_gen = (MapModifier)EditorGUILayout.ObjectField(_map_gen, typeof(MapModifier), true);
		if (_map_gen != null)
		{
			EditorGUILayout.BeginHorizontal();
			GUI.enabled = !Application.isPlaying;
			if (GUILayout.Button("Apply modifier"))
			{
				_map_gen.ApplyModifiers(_grid);
				Selection.SetActiveObjectWithContext(_grid, null);
			}
			GUI.enabled = true;

			if (GUILayout.Button("Remove doubles modifiers"))
			{
				_map_gen.m_ModifiersList = _map_gen.m_ModifiersList.Distinct().ToList();
				EditorUtility.SetDirty(_map_gen);
			}
			EditorGUILayout.EndHorizontal();
			_debug_start_modifiers = EditorGUILayout.Toggle("Debug start modifiers", _debug_start_modifiers);

			FuncEditor.DrawUILine(Color.gray);
			_edit_transformations = EditorGUILayout.Foldout(_edit_transformations, "Edit modifiers :");
			FuncEditor.DrawUILine(Color.gray);
			if (_edit_transformations)
			{
				Editor editor = Editor.CreateEditor(_map_gen);
				EditorGUILayout.BeginVertical();
				_scroll_position = EditorGUILayout.BeginScrollView(_scroll_position, false, true);
				editor.OnInspectorGUI();
				EditorGUILayout.EndScrollView();
				EditorGUILayout.EndVertical();
			}
		}
	}

	#endregion
}
