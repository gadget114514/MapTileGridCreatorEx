using System.Collections.Generic;

using MapTileGridCreator.Core;
using MapTileGridCreator.Utilities;

using NUnit.Framework;

using UnityEditor.SceneManagement;

using UnityEngine;

namespace MapTileGridCreator.Tests
{

	internal class CubeGridTest : ITestEditor
	{
		private Grid3D _grid;

		private delegate int GetRandomIntTest();
		private GetRandomIntTest _ran_int;

		private Cell CreateCell()
		{
			GameObject obj = GameObject.Instantiate(new GameObject(), _grid.transform);
			Cell cell = obj.AddComponent<Cell>();
			Assert.IsNotNull(cell);
			return cell;
		}

		/////////////////////////////////////////
		/// Tests

		//TODO False tests + tests gaps and size + test position map
		//TODO Test performances

		[OneTimeSetUp]
		public void Init()
		{
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
			_ran_int = () => { return UnityEngine.Random.Range(-100, 100); };
		}

		[SetUp]
		public void BeforeTest()
		{
			_grid = FuncEditor.InstantiateGrid3D(TypeGrid3D.Cube);
			_grid.Initialize();
		}

		[Test]
		public void Test_Init()
		{
			Assert.IsNotNull(_grid);
			Assert.IsTrue(_grid.GetAxes().Count != 0);
			Assert.IsTrue(_grid.GetConnexAxes().Count != 0);
			Assert.AreEqual((_grid.GetConnexAxes().Count), (_grid.GetAxes().Count));
		}

		[Test]
		public void Test_AddAndGetCell_False()
		{
			Vector3Int index = new Vector3Int(_ran_int(), _ran_int(), _ran_int());
			Cell cell = _grid.TryGetCellByIndex(ref index);
			Assert.IsNull(cell);

			cell = CreateCell();
			_grid.AddCell(index, cell);
			cell = _grid.TryGetCellByIndex(ref index);
			Assert.IsNotNull(cell);
		}

		[Test]
		public void Test_GetCellByIndex_Exception()
		{
			Vector3Int index = new Vector3Int(_ran_int(), _ran_int(), _ran_int());
			try
			{
				_grid.GetCellByIndex(ref index);
			}
			catch (KeyNotFoundException)
			{
				return;
			}
			Assert.That(true, "No exception on null cell GetCellByIndex.");
		}

		[Test]
		public void Test_AddAndGetCell_Random()
		{
			Vector3Int index = new Vector3Int(_ran_int(), _ran_int(), _ran_int());
			Cell cell_1 = CreateCell();
			_grid.AddCell(index, cell_1);
			Cell cell_2 = _grid.TryGetCellByIndex(ref index);
			Assert.AreSame(cell_1, cell_2);
		}

		[Test]
		public void Test_GetIndexByPosition_False()
		{
			_grid.SizeCell = 1;
			_grid.GapRatio = 0;

			Vector3 position = new Vector3(_ran_int(), _ran_int(), _ran_int());

			Cell cell_1 = CreateCell();
			cell_1.transform.localPosition = position + Vector3.one + _ran_int() * Vector3.one;
			_grid.AddCellByPosition(cell_1);
			Vector3Int index2 = _grid.GetIndexByPosition(ref position);
			Assert.AreNotEqual(cell_1.GetIndex(), index2);
		}

		[Test]
		public void Test_GetIndexByPosition_Unit1()
		{
			_grid.SizeCell = 1;
			_grid.GapRatio = 0;

			Vector3 position = new Vector3(_ran_int(), _ran_int(), _ran_int());

			Cell cell_1 = CreateCell();
			cell_1.transform.localPosition = position;
			_grid.AddCellByPosition(cell_1);
			Vector3Int index2 = _grid.GetIndexByPosition(ref position);
			Assert.AreEqual(cell_1.GetIndex(), index2);
		}

		[Test]
		public void Test_GetIndexByPosition_RandomUnit()
		{
			_grid.SizeCell = _ran_int();
			_grid.GapRatio = _ran_int();

			Vector3 position = new Vector3(_ran_int(), _ran_int(), _ran_int()) * _grid.SizeCell;

			Cell cell_1 = CreateCell();
			cell_1.transform.localPosition = position;
			_grid.AddCellByPosition(cell_1);
			Vector3Int index2 = _grid.GetIndexByPosition(ref position);
			Assert.AreEqual(cell_1.GetIndex(), index2);
		}

		[Test]
		public void Test_AddAndGetCell_PositionUnit1()
		{
			_grid.SizeCell = 1;
			_grid.GapRatio = 0;

			Vector3 position = new Vector3(_ran_int(), _ran_int(), _ran_int());

			Cell cell_1 = CreateCell();
			cell_1.transform.localPosition = position;
			_grid.AddCellByPosition(cell_1);

			Vector3Int index = _grid.GetIndexByPosition(ref position);
			Cell cell_2 = _grid.TryGetCellByIndex(ref index);
			Assert.AreSame(cell_1, cell_2);
		}

		[Test]
		public void Test_AddAndGetCell_Position()
		{
			_grid.SizeCell = _ran_int();
			_grid.GapRatio = _ran_int();

			Vector3 position = new Vector3(_ran_int(), _ran_int(), _ran_int()) * _grid.SizeCell;

			Cell cell_1 = CreateCell();
			cell_1.transform.localPosition = position;
			_grid.AddCellByPosition(cell_1);
			Vector3Int index2 = cell_1.GetIndex();
			Cell cell_2 = _grid.TryGetCellByIndex(ref index2);
			Assert.AreSame(cell_1, cell_2);
		}


		[Test]
		public void Test_AddCellByPosition_Unit1()
		{
			_grid.SizeCell = 1;
			_grid.GapRatio = 0;

			Vector3 position = new Vector3(_ran_int(), _ran_int(), _ran_int());

			Cell cell = CreateCell();
			cell.transform.localPosition = position;
			_grid.AddCellByPosition(cell);
			Vector3Int index = cell.GetIndex();
			Vector3 pos = _grid.GetLocalPositionCell(ref index);
			Assert.AreEqual(position, pos);
		}
		[Test]
		public void Test_TransformPositionToGridPosition()
		{
			Vector3 position = new Vector3(_ran_int(), _ran_int(), _ran_int());
			_grid.transform.position += position;
			_grid.SizeCell = _ran_int();
			_grid.GapRatio = _ran_int();

			Vector3 positionCell = new Vector3(_ran_int(), _ran_int(), _ran_int()) * (_grid.SizeCell * _grid.GapRatio);
			Vector3 pos = _grid.TransformPositionToGridPosition(position + positionCell) - position;
			Assert.AreEqual(positionCell, pos);
		}

		[Test]
		public void Test_AddCellByPosition_RandomUnit()
		{
			_grid.SizeCell = _ran_int();
			_grid.GapRatio = _ran_int();

			Vector3 position = new Vector3(_ran_int(), _ran_int(), _ran_int()) * (_grid.SizeCell * _grid.GapRatio);

			Cell cell = CreateCell();
			cell.transform.localPosition = position;
			_grid.AddCellByPosition(cell);
			Vector3Int index = cell.GetIndex();
			Vector3 pos = _grid.GetLocalPositionCell(ref index);
			Assert.AreEqual(position, pos);
		}

		[Test]
		public void Test_TryGetCellByPosition()
		{
			_grid.SizeCell = _ran_int();
			_grid.GapRatio = _ran_int();

			Vector3 position = new Vector3(_ran_int(), _ran_int(), _ran_int()) * (_grid.SizeCell * _grid.GapRatio);

			Cell cell = CreateCell();
			cell.transform.localPosition = position;
			_grid.AddCellByPosition(cell);
			Cell cell2 = _grid.TryGetCellByPosition(ref position);
			Assert.AreEqual(cell, cell2);
		}

		[Test]
		public void Test_GetPositionCell()
		{
			Vector3 origin = new Vector3(_ran_int(), _ran_int(), _ran_int());
			_grid.SizeCell = _ran_int();
			_grid.GapRatio = _ran_int();

			Vector3 position = new Vector3(_ran_int(), _ran_int(), _ran_int()) * (_grid.SizeCell * _grid.GapRatio) + origin;

			Cell cell = CreateCell();
			cell.transform.localPosition = position;
			_grid.AddCellByPosition(cell);
			Vector3 pos2 = _grid.GetPositionCell(cell.GetIndex());
			Assert.AreEqual(position, pos2);
		}

		[Test]
		public void Test_ReplaceCell()
		{
			Vector3Int index = new Vector3Int(_ran_int(), _ran_int(), _ran_int());
			Cell cell_1 = CreateCell();
			_grid.AddCell(index, cell_1);

			Cell cell_2 = CreateCell();
			_grid.ReplaceCell(index, cell_2);
			Assert.AreNotSame(cell_1, cell_2);

			Cell cell_3 = _grid.TryGetCellByIndex(ref index);
			Assert.AreSame(cell_2, cell_3);
		}

		[Test]
		public void Test_DeleteCell()
		{
			Vector3Int index = new Vector3Int(_ran_int(), _ran_int(), _ran_int());
			Assert.IsNull(_grid.TryGetCellByIndex(ref index));

			Cell cell_1 = CreateCell();
			_grid.AddCell(index, cell_1);
			Assert.IsNotNull(_grid.TryGetCellByIndex(ref index));

			_grid.DeleteCell(cell_1);
			Assert.IsNull(_grid.TryGetCellByIndex(ref index));
		}

		[Test]
		public void Test_GetConnexIndexAxes()
		{
			Assert.IsNotEmpty(_grid.GetConnexAxes());
		}

		[Test]
		public void Test_GetAxes()
		{
			Assert.IsNotEmpty(_grid.GetAxes());
		}

		[Test]
		public void Test_GetNeighboursCell()
		{
			Vector3Int index = new Vector3Int(_ran_int(), _ran_int(), _ran_int());
			Assert.IsEmpty(_grid.GetNeighboursCell(ref index));

			Cell cell = CreateCell();
			_grid.AddCell(index, cell);

			foreach (Vector3Int v in _grid.GetNeighboursIndex(ref index))
			{
				Cell cellv = CreateCell();
				_grid.AddCell(v, cellv);
			}
			Assert.IsNotEmpty(_grid.GetNeighboursCell(ref index));
			Assert.True(_grid.GetNeighboursCell(ref index).Count == _grid.GetNeighboursIndex(ref index).Count);
		}

		[Test]
		public void Test_GetNeighboursIndex()
		{
			Vector3Int index = new Vector3Int(_ran_int(), _ran_int(), _ran_int());
			Assert.IsNotEmpty(_grid.GetNeighboursIndex(ref index));
		}

		[TearDown]
		public void AfterTest()
		{
			GameObject.DestroyImmediate(_grid.gameObject);
		}

		[OneTimeTearDown]
		public void End()
		{
			_ran_int = null;
		}

	}
}
