using FishNet.Object;
using TinyTanks.Tanks;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace TinyTanks.AI
{
    [RequireComponent(typeof(TankController))]
    public class TankAI : NetworkBehaviour
    {
        public float corneringDistance = 25f;
        public float maxTurretDelta = 70f;
        public float targetAcquisitionTime = 0.4f;

        private TankController tank;
        private TankController target;
        private NavMeshPath path;
        private float pathTimer;
        private float targetAcquisitionTimer;

        private void Awake()
        {
            tank = GetComponent<TankController>();
            path = new NavMeshPath();
        }

        private void FixedUpdate()
        {
            if (!HasAuthority) return;

            if (target == null && TankController.all.Count > 1)
            {
                var randomIndex = Random.Range(0, TankController.all.Count - 1);
                var thisIndex = TankController.all.IndexOf(tank);
                if (thisIndex == randomIndex) randomIndex += 1;
                target = TankController.all[randomIndex];
            }

            if (target != null)
            {
                if (pathTimer < 0f)
                {
                    NavMesh.CalculatePath(tank.transform.position, target.transform.position, ~0, path);
                    pathTimer = 0.1f;
                }

                if (CanSeeTarget() || path.corners.Length == 2)
                {
                    tank.throttle = 0f;
                    tank.steering = 0f;

                    var targetDirection = (target.model.turretMount.position - tank.model.gunPivot.position).normalized;
                    MoveTurretTowards(targetDirection);

                    if (Mathf.Acos(Vector3.Dot(targetDirection, tank.model.gunPivot.forward)) * Mathf.Rad2Deg < 1f)
                    {
                        targetAcquisitionTimer += Time.deltaTime;
                    }
                    else
                    {
                        targetAcquisitionTimer = 0f;
                    }

                    tank.SetStabs(true);

                    if (targetAcquisitionTimer >= targetAcquisitionTime)
                    {
                        tank.StartShooting(0);
                    }
                    else
                    {
                        tank.StopShooting(0);
                    }
                }
                else if (path.corners.Length > 2)
                {
                    var vector = path.corners[1] - path.corners[0];
                    tank.steering = Vector3.Dot(Vector3.Cross(tank.transform.forward, vector.normalized), tank.transform.up);
                    tank.throttle = Vector3.Dot(vector.normalized, tank.transform.forward) * Mathf.Clamp(vector.magnitude / corneringDistance, 0.2f, 1f);

                    tank.SetStabs(false);
                    MoveTurretTowards(tank.transform.forward);
                    
                    tank.StopShooting(0);
                }
            }

            pathTimer -= Time.deltaTime;
        }

        private bool CanSeeTarget()
        {
            var ray = new Ray(tank.model.turretMount.position, (target.model.turretMount.position - tank.model.turretMount.position).normalized);
            var query = Physics.RaycastAll(ray);

            var bestHit = (RaycastHit?)null;

            foreach (var hit in query)
            {
                if (bestHit.HasValue && bestHit.Value.distance < hit.distance) continue;
                if (hit.collider.transform.IsChildOf(tank.transform)) continue;
                bestHit = hit;
            }

            if (bestHit.HasValue) Debug.DrawLine(ray.origin, bestHit.Value.point, Color.red);
            else Debug.DrawLine(ray.origin, ray.GetPoint(1024f), Color.red);

            return bestHit.HasValue && bestHit.Value.collider.transform.IsChildOf(target.transform);
        }

        private void MoveTurretTowards(Vector3 targetDirection)
        {
            var orientation = Quaternion.LookRotation(tank.transform.InverseTransformVector(targetDirection).normalized, tank.transform.up);
            var rotation = new Vector2(orientation.eulerAngles.y, -orientation.eulerAngles.x);

            tank.turretTraverse = new Vector2
            {
                x = Mathf.MoveTowardsAngle(tank.turretRotation.x, rotation.x, maxTurretDelta * Time.fixedDeltaTime),
                y = Mathf.MoveTowardsAngle(tank.turretRotation.y, rotation.y, maxTurretDelta * Time.fixedDeltaTime),
            } - tank.turretRotation;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            if (path != null)
            {
                for (var i = 0; i < path.corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
                    Gizmos.DrawWireSphere(path.corners[i + 1], 0.1f);
                }
            }
        }
    }
}