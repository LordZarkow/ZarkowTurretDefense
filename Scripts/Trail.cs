using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZarkowTurretDefense.Scripts
{
    using UnityEngine;

    // class to hold data describing game object and line renderer class, handling animation of it, adding points based on calls to Update()
    public class Trail : MonoBehaviour
    {
        private readonly float _intervalTime = 0.05f;

        public GameObject ParentGameObject;

        private LineRenderer _lineRenderer;

        private readonly List<Vector3> _listLocations;
        private readonly int _maxPoints = 30;

        private float _intervalTimerCounter;

        private Transform _transform;

        private bool _enabled;

        public Trail()
        {
            _listLocations = new List<Vector3>();
        }

        void Awake()
        {
            _lineRenderer = transform.GetComponentInChildren<LineRenderer>();

            if (_lineRenderer == null)
            {
                Jotunn.Logger.LogDebug($"{DateTime.Now:o} ### Trail[{gameObject.GetInstanceID()}]: No LineRenderer found");
                return;
            }

            _transform = this.transform;
            _intervalTimerCounter = 0;
            _enabled = true;
        }

        public void InitLocationListAtStartLocation(Vector3 startLocation)
        {
            _listLocations.Clear();
            for (int i = 0; i < _maxPoints - 1; i++)
            {
                _listLocations.Add(startLocation);
            }
        }

        void Update()
        {
            if (_enabled == false)
                return;

            _intervalTimerCounter -= Time.deltaTime;

            if (_intervalTimerCounter <= 0.0f)
            {
                AddLocationPoint(_transform.position);

                _intervalTimerCounter += _intervalTime;
            }
        }


        public void AddLocationPoint(Vector3 location)
        {
            _listLocations.Add(location);

            CountOutLines();
        }

        private void CountOutLines()
        {
            // remove most oldest location, when longer than NN segments
            if (_listLocations.Count > _maxPoints)
            {
                _listLocations.RemoveAt(0);
            }

            // set points
            _lineRenderer.positionCount = _listLocations.Count;
            _lineRenderer.SetPositions(_listLocations.ToArray());
        }

        public void DisableTrail()
        {
            _enabled = true;
        }
    }
}
