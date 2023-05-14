using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class V3M
{
	static public Vector3 mult(Vector3 a, Vector3 b) { return new Vector3(a.x*b.x, a.y*b.y, a.z*b.z); }
	static public Vector3 div(Vector3 a, Vector3 b) { return new Vector3(a.x/b.x, a.y/b.y, a.z/b.z); }
	static public Vector3 add(Vector3 a, Vector3 b) { return new Vector3(a.x+b.x, a.y+b.y, a.z+b.z); }

}
