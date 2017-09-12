using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BallManager
{
	[HideInInspector] public int m_BallNum;
	[HideInInspector] public GameObject m_Instance;
	[HideInInspector] public List<Vector2> m_Goals;
	[HideInInspector] public float m_MaxSpeed;
	[HideInInspector] public float m_AttackRange;


}

