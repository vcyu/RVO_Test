using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

	public GameObject m_BallPrefab;

	private List<BallManager> m_Balls = new List<BallManager> ();
	private float m_BaseSpeed = 1.0f;
	private float m_SpeedRange = 0.1f;
	private float m_edge = 37f;
	private float m_distance = 35f;
	private float[] range_array = { 2f, 5f, 10f, 15f };
	private float targetRangeMax = 10f;
	private float targetRangeMin = -10f;
    private PathFinder pathFinder;

	// Use this for initialization
	void Start () {
        float cellRange = m_distance + 1f;
        pathFinder = new PathFinder(-cellRange, cellRange, -cellRange, cellRange, 1f);
        pathFinder.AddBlock(new Rect(-5, -5, 10, 10));
        pathFinder.InitCells();

		RVO.Simulator.Instance.setTimeStep(0.25f);
		RVO.Simulator.Instance.setAgentDefaults (10f, 40, 10f, 1.5f, 1f, 1f, new RVO.Vector2 (0f, 0f)); 

		IList<RVO.Vector2> obstacle1 = new List<RVO.Vector2>();
		obstacle1.Add(new RVO.Vector2(m_edge, m_edge));
		obstacle1.Add(new RVO.Vector2(m_edge, -m_edge));
		RVO.Simulator.Instance.addObstacle(obstacle1);

		IList<RVO.Vector2> obstacle2 = new List<RVO.Vector2>();
		obstacle2.Add(new RVO.Vector2(m_edge, -m_edge));
		obstacle2.Add(new RVO.Vector2(-m_edge, -m_edge));
		RVO.Simulator.Instance.addObstacle(obstacle2);

		IList<RVO.Vector2> obstacle3 = new List<RVO.Vector2>();
		obstacle3.Add(new RVO.Vector2(-m_edge, -m_edge));
		obstacle3.Add(new RVO.Vector2(-m_edge, m_edge));
		RVO.Simulator.Instance.addObstacle(obstacle3);

		IList<RVO.Vector2> obstacle4 = new List<RVO.Vector2>();
		obstacle4.Add(new RVO.Vector2(-m_edge, m_edge));
		obstacle4.Add(new RVO.Vector2(m_edge, m_edge));
		RVO.Simulator.Instance.addObstacle(obstacle4);

		RVO.Simulator.Instance.processObstacles();

		for (int k = 0; k <= 2; k += 1) {
			for (float i = -10f; i <= 10; i += 4f) {
				//for (int j = -1; j <= 1; j += 2) {
				//	CreateBall (new Vector3 (j * (m_distance-(float)k), 0.5f, (float)i));
				//}
				CreateBall (new Vector3 ((m_distance-(float)k), 0.5f, (float)i));
			}
		}
	}

	void CreateBall(Vector3 pos){
        // 创建小球
		BallManager ball = new BallManager ();
		ball.m_Instance = 
			Instantiate (m_BallPrefab, pos, new Quaternion(0f, 0f, 0f, 0f)) as GameObject;

		// 设定目标
		//ball.m_Goal = new Vector2 (0f, 0f);
		//ball.m_Goal = new Vector2 (-pos.x, -pos.z);
		Vector2 final_goal = new Vector2((pos.x > 0 ? -m_distance : m_distance), GetFitInRange(pos.z));
        ball.m_Goals = pathFinder
        // 设定速度
		ball.m_MaxSpeed = Random.Range (m_BaseSpeed - m_SpeedRange, m_BaseSpeed + m_SpeedRange);
        // 设定攻击距离
		ball.m_AttackRange = range_array [(int)Random.Range (0, range_array.Length - 1)];
        // 设定小球id
		ball.m_BallNum = RVO.Simulator.Instance.addAgent (new RVO.Vector2 (ball.m_Instance.transform.position.x, ball.m_Instance.transform.position.z));
        // 传送到RVO系统
		RVO.Simulator.Instance.setAgentMaxSpeed(ball.m_BallNum, ball.m_MaxSpeed);

		m_Balls.Add (ball);
	}

	
	// Update is called once per frame
	void Update () {
		UpdateBallPos ();
		RVO.Simulator.Instance.doStep();
	}

	void UpdateBallPos()
	{
		foreach (BallManager ball in m_Balls) {
			// 根据模拟结果，设置小球位置
			RVO.Vector2 curPos = RVO.Simulator.Instance.getAgentPosition (ball.m_BallNum);
			RVO.Vector2 lastPos = new RVO.Vector2 (ball.m_Instance.transform.position.x, ball.m_Instance.transform.position.z);
			ball.m_Instance.transform.position = new Vector3(curPos.x(), 0.5f, curPos.y());

			// 设置小球下一步目标
			RVO.Vector2 goalVector = new RVO.Vector2(ball.m_Goal.x, ball.m_Goal.y) - curPos;
			RVO.Simulator.Instance.setAgentMaxSpeed (ball.m_BallNum, ball.m_MaxSpeed);

			if (RVO.RVOMath.absSq (goalVector) < ball.m_AttackRange*ball.m_AttackRange) {
				if (RVO.RVOMath.absSq (goalVector) <= ball.m_AttackRange*ball.m_AttackRange * 0.9f) {
					RVO.Simulator.Instance.setAgentMaxSpeed (ball.m_BallNum, ball.m_MaxSpeed * 0.2f);
				}

				RVO.Simulator.Instance.setAgentPrefVelocity(ball.m_BallNum, new RVO.Vector2 (0f, 0f));
				if (RVO.RVOMath.absSq(lastPos - curPos) / (Time.deltaTime * Time.deltaTime) <= ball.m_MaxSpeed * ball.m_MaxSpeed * 0.04)
					print (string.Format ("Ball {0:d} is on attack, attack range is {1:f}.", ball.m_BallNum, ball.m_AttackRange));
				else
					print (string.Format ("Ball {0:d} is on position, attack range is {1:f}.", ball.m_BallNum, ball.m_AttackRange));
			}
			else {
				if (RVO.RVOMath.absSq (goalVector) > 1.0f) {
					goalVector = RVO.RVOMath.normalize (goalVector);
				}
				RVO.Simulator.Instance.setAgentPrefVelocity(ball.m_BallNum, goalVector);

				/* Perturb a little to avoid deadlocks due to perfect symmetry. */
				float angle = (float)Random.value * 2.0f * Mathf.PI;
				float dist = (float)Random.value * 0.0001f;

				RVO.Simulator.Instance.setAgentPrefVelocity(ball.m_BallNum, RVO.Simulator.Instance.getAgentPrefVelocity(ball.m_BallNum) +
					dist * new RVO.Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));

				print (string.Format ("Ball {0:d} is on move, attack range is {1:f}.", ball.m_BallNum, ball.m_AttackRange));
			}
				
		}

	}

	float GetFitInRange(float x)
	{
		if (x > targetRangeMax) {
			return targetRangeMax;
		} else if (x < targetRangeMin) {
			return targetRangeMin;
		}

		return x;
	}

    void AdjustGoal(BallManager ball)
    {
        List<BallManager>[] ballGroups;
        foreach (BallManager other_ball in m_Balls)
        {
            if (other_ball.m_AttackRange == ball.m_AttackRange && other_ball.m_Goal.x == ball.m_Goal.x)
            {
                
            }
        }
    }


}
