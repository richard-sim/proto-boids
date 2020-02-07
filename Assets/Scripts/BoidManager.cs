using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoidManager : MonoBehaviour
{
    public GameObject prefab;

    public int numToSpawn = 10;
    public float spawnRadius = 1.0f;

    public float boundsRadius = 10.0f;
    public float boundsPow = 10.0f;

    public float neighbourhoodRadius = 0.2f;

    public float separationFactor = 0.2f;
    public float alignmentFactor = 0.1f;
    public float cohesionFactor = 0.2f;

    public float speed = 1.0f;
    public float leadershipChangeChance = 0.25f;

    private GameObject[] _boids;
    private GameObject _leader;

    private Collider[] _neighbours;
    
    void Start()
    {
        Random.InitState(System.DateTime.Now.Millisecond);
        
        _boids = new GameObject[numToSpawn];
        for (int i = 0; i < numToSpawn; i++)
        {
            Vector3 pos = this.transform.position + Random.insideUnitSphere * spawnRadius;
            _boids[i] = GameObject.Instantiate(prefab, pos, Random.rotationUniform, this.transform);
        }

        _leader = null;
        
        _neighbours = new Collider[numToSpawn];
    }

    void Update()
    {
        if (!(_leader is null))
        {
            _leader.transform.position += _leader.transform.forward * (speed * Time.deltaTime);
        }

        float frameAlignmentFactor = alignmentFactor * Time.deltaTime;
        float frameSeparationFactor = separationFactor * Time.deltaTime;
        float frameCohesionFactor = cohesionFactor * Time.deltaTime;

        foreach (GameObject boid in _boids)
        {
            if (boid == _leader)
            {
                continue;
            }

            Transform boidTransform = boid.transform;
            Vector3 boidPos = boidTransform.position;
            int numNeighbours = Physics.OverlapSphereNonAlloc(boidPos, neighbourhoodRadius, _neighbours);

            Quaternion alignment = boidTransform.rotation;

            // There will always be at least one neighbour, the boid itself
            if (numNeighbours > 1)
            {
                Vector3 separationDir = Vector3.zero;
                Vector3 cohesionPos = Vector3.zero;

                for (int i = 0; i < numNeighbours; i++)
                {
                    Collider neighbour = _neighbours[i];
                    GameObject go = neighbour.gameObject;
                    if (go == boid)
                    {
                        continue;
                    }

                    Vector3 neighbourPos = go.transform.position;
                    Quaternion neighbourRot = go.transform.rotation;

                    Vector3 neighbourBoidDir = boidPos - neighbourPos;
                    float mag = Mathf.Clamp01(1.0f - (neighbourBoidDir.magnitude / neighbourhoodRadius)) * neighbourhoodRadius;

                    separationDir += neighbourBoidDir.normalized * mag;
                    cohesionPos += neighbourPos;
                    alignment = Quaternion.Slerp(alignment, neighbourRot, frameAlignmentFactor);
                }
                cohesionPos /= numNeighbours - 1;

                if (separationDir.sqrMagnitude > Mathf.Epsilon)
                {
                    Quaternion separationRot = Quaternion.LookRotation(separationDir);
                    float sf = frameSeparationFactor * separationDir.magnitude;
                    
                    alignment = Quaternion.Slerp(alignment, separationRot, sf);
                }

                Vector3 cohesionDir = cohesionPos - boidPos;
                if (cohesionDir.sqrMagnitude > Mathf.Epsilon)
                {
                    Quaternion cohesionRot = Quaternion.LookRotation(cohesionDir);
                    float cf = frameCohesionFactor * cohesionDir.magnitude;

                    alignment = Quaternion.Slerp(alignment, cohesionRot, cf);
                }
            }

            Vector3 centerDir = -(boidPos - this.transform.position);
            float centerSteerStrength = Mathf.Pow(Mathf.Clamp01(centerDir.magnitude / boundsRadius), boundsPow);
            Quaternion centerSteerRot = Quaternion.LookRotation(centerDir);
            alignment = Quaternion.Slerp(alignment, centerSteerRot, centerSteerStrength * Time.deltaTime);
            
            boidTransform.rotation = alignment;
            boidTransform.position += boidTransform.forward * (speed * Time.deltaTime);
        }

        if (Random.Range(0.0f, 1.0f) < (leadershipChangeChance * Time.deltaTime))
        {
            // Mostly choose no leader
            if (Random.Range(0.0f, 1.0f) < leadershipChangeChance)
            {
                _leader = _boids[Random.Range(0, _boids.Length - 1)];
        
                _leader.transform.rotation = Random.rotationUniform;
            }
            else
            {
                _leader = null;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (_boids != null)
        {
            foreach (GameObject boid in _boids)
            {
                if (boid == _leader)
                    continue;

                Debug.DrawRay(boid.transform.position, boid.transform.forward, Color.blue);
            }
        }

        if (!(_leader is null))
        {
            Debug.DrawRay(_leader.transform.position, _leader.transform.forward, Color.red);
        }
    }
}
