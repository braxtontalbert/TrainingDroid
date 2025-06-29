using System;
using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using ThunderRoad.DebugViz;
using TOR;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;
using Random = System.Random;

namespace TrainingDroid
{
    public class TrainingDroid : MonoBehaviour
    {
        private Item item;
        private bool trainingDroidActive;
        private bool sentryDroidActive;
        Vector3 targetPosition;
        Quaternion targetRotation;
        private PID pid;
        List<Transform> bulletSpawns = new List<Transform>();
        private float maxSecondsToShoot = 3f;
        private Transform currentBulletSpawn;
        private AudioSource source;
        private AudioContainer alternate;
        private int lastIndex;
        private AudioSource moveSound;
        GameObject targetObject;
        private NavMeshAgent agent;
        private void Start()
        {
            item = GetComponent<Item>();
            moveSound = item.gameObject.GetComponent<AudioSource>();
            item.OnHeldActionEvent += HeldActionEvent;
            item.OnGrabEvent += GrabEvent;
            pid = new PID();
            pid.proportionalGain = 5f;
            pid.derivativeGain = 1.3f;
            pid.integralGain = 0f;
            pid.derivativeMeasurement = PID.DerivativeMeasurement.ErrorRateOfChange;
            var masterTransform = item.GetCustomReference<Transform>("SpawnPoints");
            foreach (Transform child in masterTransform)
            {
                bulletSpawns.Add(child);
            }
            ItemData soundData = Catalog.GetData<ItemData>("Blaster_Defender");
            soundData.SpawnAsync(callback =>
            {
                callback.transform.position = new Vector3(0, 10000f, 10000f);
                var data = callback.data.modules[0] as ItemModuleBlaster;
                alternate = data?.fireSoundAsset;
                source = callback.GetCustomReference(data?.fireSoundID)
                    .GetComponent<AudioSource>();
                
            });
            DroidOptions.local.droids.Add(this);
            targetObject = new GameObject();
            //targetObject = Instantiate(refObject);
            SetupNavMeshAgent();
        }

        private void GrabEvent(Handle handle, RagdollHand ragdollhand)
        {
            if (source) return;
            ItemData soundData = Catalog.GetData<ItemData>("Blaster_Defender");
            soundData.SpawnAsync(callback =>
            {
                callback.transform.position = new Vector3(0, 10000f, 10000f);
                var data = callback.data.modules[0] as ItemModuleBlaster;
                alternate = data?.fireSoundAsset;
                source = callback.GetCustomReference(data?.fireSoundID)
                    .GetComponent<AudioSource>();
                
            });
        }

        private void SetupNavMeshAgent(float speedMultiplier = 5f, float stoppingDistance = 1f, float baseOffset = 5f)
        {
            targetObject.transform.position = transform.position;
            targetObject.transform.localScale = Vector3.one * 0.5f;
            agent = targetObject.gameObject.AddComponent<NavMeshAgent>();
            agent.stoppingDistance = stoppingDistance;
            agent.speed *= speedMultiplier;
            agent.baseOffset = baseOffset;
            agent.autoBraking = true;
        }

        Quaternion FindRandomDirection(Vector3 worldTargetPoint, Item parent, Transform child)
        {
            Vector3 dirToTarget = worldTargetPoint - child.position;
            if (dirToTarget.sqrMagnitude < 0.01f) return Quaternion.identity;

            dirToTarget.Normalize();
            Quaternion desiredChildRotation = Quaternion.LookRotation(dirToTarget, Vector3.up);
            Quaternion delta = desiredChildRotation * Quaternion.Inverse(child.rotation);
            return delta * parent.transform.rotation;
        }
        private void OnDestroy()
        {
            item.OnHeldActionEvent -= HeldActionEvent;
        }

        private bool coroutinesRunning = false;
        private bool coroutinesSentryRunning = false;
        private Coroutine currentUpdatePositionCoroutine;
        private Coroutine blasterTargetCoroutine;
        private Coroutine currentSentryUpdatePositionCoroutine;
        private Coroutine blasterSentryTargetCoroutine;
        private void HeldActionEvent(RagdollHand ragdollhand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.AlternateUseStart && !sentryDroidActive)
            {
                trainingDroidActive = !trainingDroidActive;
                item.physicBody.rigidBody.useGravity = !item.physicBody.rigidBody.useGravity;
                if (trainingDroidActive)
                {
                    StopAllCoroutinesSafe();
                    currentUpdatePositionCoroutine = StartCoroutine(UpdatePosition());
                    blasterTargetCoroutine = StartCoroutine(ShootBlasterAtTarget());
                }

            }
            else if (action == Interactable.Action.UseStart && !trainingDroidActive)
            {
                sentryDroidActive = !sentryDroidActive;
                item.physicBody.rigidBody.useGravity = !item.physicBody.rigidBody.useGravity;
                if (sentryDroidActive)
                {
                    StopAllCoroutinesSafe();
                    currentSentryUpdatePositionCoroutine = StartCoroutine(UpdateSentryPosition());
                    blasterSentryTargetCoroutine = StartCoroutine(ShootSentryBlasterAtTarget());
                }
            }
        }

        void StopAllCoroutinesSafe()
        {
            if (currentUpdatePositionCoroutine != null) StopCoroutine(currentUpdatePositionCoroutine);
            if (blasterTargetCoroutine != null) StopCoroutine(blasterTargetCoroutine);
            if (currentSentryUpdatePositionCoroutine != null) StopCoroutine(currentSentryUpdatePositionCoroutine);
            if (blasterSentryTargetCoroutine != null) StopCoroutine(blasterSentryTargetCoroutine);

            currentUpdatePositionCoroutine = null;
            blasterTargetCoroutine = null;
            currentSentryUpdatePositionCoroutine = null;
            blasterSentryTargetCoroutine = null;

            coroutinesRunning = false;
            coroutinesSentryRunning = false;
        }
        void ShootBlaster(Transform bulletSpawn)
        {
            var stringData = "BlasterBolt"+DroidOptions.currentColor;
            ProjectileData data = Catalog.GetData<ProjectileData>(stringData);
            ItemData itemData = Catalog.GetData<ItemData>(data.item);
            var activeDamager = data.damager;
            Action<Item> callback = (Action<Item>) (projectile =>
            {
                source.transform.parent = item.transform;
                source.transform.position = item.transform.position;
                source.PlayOneShot(alternate ? alternate.PickAudioClip() : source.clip);
                NoiseManager.AddNoise(source.transform.position, source.volume);
              ItemBlasterBolt component1 = projectile.gameObject.GetComponent<ItemBlasterBolt>();
              if ((UnityEngine.Object) component1 != (UnityEngine.Object) null)
                component1.UpdateValues(ref data);
              CollisionIgnoreHandler collisionIgnoreHandler = projectile.gameObject.GetComponent<CollisionIgnoreHandler>();
              if (!(bool) (UnityEngine.Object) collisionIgnoreHandler)
                collisionIgnoreHandler = projectile.gameObject.AddComponent<CollisionIgnoreHandler>();
              if (!projectile.gameObject.activeInHierarchy)
                projectile.gameObject.SetActive(true);
              collisionIgnoreHandler.item = projectile;
              collisionIgnoreHandler.IgnoreCollision(this.item);
              foreach (CollisionHandler collisionHandler in projectile.collisionHandlers)
              {
                collisionHandler.SetPhysicModifier((object) this, new float?((float) (data.useGravity ? 1 : 0)), drag: data.drag);
                if (activeDamager != null)
                {
                  foreach (Damager damager in collisionHandler.damagers)
                    damager.data = activeDamager;
                }
              }
              Transform transform2 = projectile.transform;
              transform2.position = bulletSpawn.position; 
              transform2.rotation = Quaternion.Euler(bulletSpawn.rotation.eulerAngles);
              Rigidbody component2 = projectile.GetComponent<Rigidbody>();
              component2.mass /= GlobalSettings.BlasterBoltSpeed *  1f;
              projectile.Throw(flyDetection: Item.FlyDetection.Forced);
              component2.AddForce(component2.transform.forward * 5000f / Time.timeScale);
              component1.trail?.Clear();
            });
            itemData.SpawnAsync(callback);
        }
        
        private void FixedUpdate()
        {
            if (trainingDroidActive && item.mainHandler == null)
            {
                agent.destination = targetPosition;
                Vector3 navigatedXZ = new Vector3(agent.nextPosition.x, targetPosition.y, agent.nextPosition.z);
                item.physicBody.rigidBody.AddForce(pid.Update(Time.fixedDeltaTime, item.transform.position, navigatedXZ) * 10f, ForceMode.Acceleration);
                if (currentBulletSpawn != null)
                {
                    Quaternion currentRot = item.physicBody.rigidBody.rotation;
                    Quaternion delta = targetRotation * Quaternion.Inverse(currentRot);

                    delta.ToAngleAxis(out float angle, out Vector3 axis);
                    if (angle > 180f) angle -= 360f;

                    if (angle > 0.1f && axis != Vector3.zero)
                    {
                        float angleRad = angle * Mathf.Deg2Rad;
                        Vector3 torque = axis.normalized * angleRad * 600f;
                        item.physicBody.rigidBody.AddTorque(torque, ForceMode.Acceleration);

                        // Optional: Damping
                        item.physicBody.rigidBody.angularVelocity *= 0.95f;
                    }
                }
            }
            else if (sentryDroidActive && item.mainHandler == null)
            {
                agent.destination = targetPosition;
                Vector3 navigatedXZ = new Vector3(agent.nextPosition.x, targetPosition.y, agent.nextPosition.z);
                item.physicBody.rigidBody.AddForce(
                    pid.Update(Time.fixedDeltaTime, item.transform.position, navigatedXZ) * 10f,
                    ForceMode.Acceleration);
                if (currentBulletSpawn != null)
                {
                    Quaternion currentRot = item.physicBody.rigidBody.rotation;
                    Quaternion delta = targetRotation * Quaternion.Inverse(currentRot);

                    delta.ToAngleAxis(out float angle, out Vector3 axis);
                    if (angle > 180f) angle -= 360f;

                    if (angle > 0.1f && axis != Vector3.zero)
                    {
                        float angleRad = angle * Mathf.Deg2Rad;
                        Vector3 torque = axis.normalized * angleRad * 600f;
                        item.physicBody.rigidBody.AddTorque(torque, ForceMode.Acceleration);

                        // Optional: Damping
                        item.physicBody.rigidBody.angularVelocity *= 0.95f;
                    }
                }
            }
        }

        private bool targetRotSet;
        IEnumerator ShootBlasterAtTarget()
        {
            while (trainingDroidActive)
            {
                int randIndex;
                do
                {
                    randIndex = UnityEngine.Random.Range(0, bulletSpawns.Count);
                } 
                while (bulletSpawns.Count > 1 && randIndex == lastIndex);
        
                lastIndex = randIndex;
                currentBulletSpawn = bulletSpawns[randIndex];
                //var randIndexPart = UnityEngine.Random.Range(0, Player.local.creature.ragdoll.parts.Count);
                var randomPart = Player.local.creature.ragdoll.targetPart;
                var target = randomPart.transform.position;
                targetRotSet = true;
                targetRotation = FindRandomDirection(target, item, currentBulletSpawn);
                if (targetRotation == Quaternion.identity)
                {
                    // Skip this shot if rotation couldn't be calculated
                    yield return null;
                    continue;
                }
                yield return RotateAndAlignToTarget(randomPart.transform.position, currentBulletSpawn);
                ShootBlaster(currentBulletSpawn);

                yield return new WaitForSeconds(UnityEngine.Random.Range(DroidOptions.diffcultyLevels[DroidOptions.difficultyIndex], DroidOptions.diffcultyLevels[DroidOptions.difficultyIndex] + (DroidOptions.diffcultyLevels.Length - (DroidOptions.difficultyIndex + DroidOptions.diffcultyLevels[DroidOptions.difficultyIndex]))));
                targetRotSet = false;
            }
        }
        
        IEnumerator ShootSentryBlasterAtTarget()
        {
            while (sentryDroidActive)
            {
                int randIndex;
                do
                {
                    randIndex = UnityEngine.Random.Range(0, bulletSpawns.Count);
                } while (bulletSpawns.Count > 1 && randIndex == lastIndex);

                lastIndex = randIndex;
                currentBulletSpawn = bulletSpawns[randIndex];
                List<Creature> nonPlayerCreatures = new List<Creature>();
                foreach (var creature in Creature.allActive)
                {
                    if (!creature.isPlayer && !creature.isKilled)
                        nonPlayerCreatures.Add(creature);
                }

                // Check if there's at least one valid target
                if (nonPlayerCreatures.Count > 0)
                {
                    var selected = nonPlayerCreatures[UnityEngine.Random.Range(0, nonPlayerCreatures.Count)];
                    //var randIndexPart = UnityEngine.Random.Range(0, Player.local.creature.ragdoll.parts.Count);
                    var randomPart = selected.ragdoll.targetPart;
                    var target = randomPart.transform.position;

                    targetRotSet = true;
                    targetRotation = FindRandomDirection(target, item, currentBulletSpawn);
                    if (targetRotation == Quaternion.identity)
                    {
                        yield return null;
                        yield break;
                    }

                    yield return RotateAndAlignToTarget(target, currentBulletSpawn);
                    ShootBlaster(currentBulletSpawn);
                    yield return new WaitForSeconds(UnityEngine.Random.Range(
                        DroidOptions.diffcultyLevels[DroidOptions.difficultyIndex],
                        DroidOptions.diffcultyLevels[DroidOptions.difficultyIndex] +
                        (DroidOptions.diffcultyLevels.Length - (DroidOptions.difficultyIndex +
                                                                DroidOptions.diffcultyLevels[
                                                                    DroidOptions.difficultyIndex]))));
                    targetRotSet = false;
                }
                else yield return null;
            }
        }

        public void ResetCoroutines()
        {
            StopCoroutine(currentUpdatePositionCoroutine);
            StopCoroutine(blasterTargetCoroutine);
            currentUpdatePositionCoroutine = StartCoroutine(UpdatePosition());
            blasterTargetCoroutine = StartCoroutine(ShootBlasterAtTarget());
        }
        
        IEnumerator RotateAndAlignToTarget(Vector3 target, Transform chosenBulletSpawn, float maxWaitTime = 2f, float angleThreshold = 3f)
        {
            targetRotation = FindRandomDirection(target, item, chosenBulletSpawn);
            if (targetRotation == Quaternion.identity)
            {
                Debug.LogWarning("Invalid rotation. Skipping shot.");
                yield break;
            }
            var randomPitch = UnityEngine.Random.Range(0.5f, 1.5f);
            moveSound.pitch = randomPitch;
            moveSound.Play();
            float timer = 0f;
            while (timer < maxWaitTime)
            {
                timer += Time.deltaTime;

                Quaternion currentRot = item.physicBody.rigidBody.rotation;
                Quaternion delta = targetRotation * Quaternion.Inverse(currentRot);
                delta.ToAngleAxis(out float angle, out Vector3 axis);
                if (angle > 180f) angle -= 360f;

                if (Mathf.Abs(angle) < angleThreshold)
                {
                    yield break; // Done rotating
                }

                if (axis != Vector3.zero)
                {
                    float angleRad = Mathf.Deg2Rad * Mathf.Clamp(Mathf.Abs(angle), 1f, 60f);
                    var speed = 600f;
                    if(DroidOptions.difficultyIndex == 5)
                        speed += (speed / DroidOptions.diffcultyLevels[DroidOptions.difficultyIndex]);
                    Vector3 torque = axis.normalized * angleRad * speed;
                    item.physicBody.rigidBody.WakeUp();
                    item.physicBody.rigidBody.AddTorque(torque, ForceMode.Acceleration);
                    float angleAbs = Mathf.Abs(angle);
                    float damping = Mathf.Lerp(0.98f, 0.90f, Mathf.InverseLerp(1f, 0.1f, angleAbs));
                    damping = Mathf.Clamp(damping, 0.90f, 0.98f);
                    item.physicBody.rigidBody.angularVelocity *= damping;
                }

                yield return new WaitForFixedUpdate();
            }

            Debug.LogWarning("Timeout while aligning rotation. Still firing.");
        }
        IEnumerator UpdatePosition()
        {
            while (trainingDroidActive)
            {
                var randomOffset = UnityEngine.Random.Range(1.5f, 8f);
                agent.baseOffset = randomOffset;
                targetPosition = GetPositionFromRandomDirection(Player.local.creature.ragdoll.targetPart.transform.forward);
                yield return new WaitForSeconds(UnityEngine.Random.Range(DroidOptions.diffcultyLevels[DroidOptions.difficultyIndex], DroidOptions.diffcultyLevels[DroidOptions.difficultyIndex] + (DroidOptions.diffcultyLevels.Length - (DroidOptions.difficultyIndex + DroidOptions.diffcultyLevels[DroidOptions.difficultyIndex]))));
            }
        }
        IEnumerator UpdateSentryPosition()
        {
            while (sentryDroidActive)
            {
                var randomOffset = UnityEngine.Random.Range(1.5f, 8f);
                agent.baseOffset = randomOffset;
                targetPosition = Player.local.creature.ragdoll.headPart.transform.position + (UnityEngine.Random.onUnitSphere * 2f);
                yield return new WaitForSeconds(UnityEngine.Random.Range(DroidOptions.diffcultyLevels[DroidOptions.difficultyIndex], DroidOptions.diffcultyLevels[DroidOptions.difficultyIndex] + (DroidOptions.diffcultyLevels.Length - (DroidOptions.difficultyIndex + DroidOptions.diffcultyLevels[DroidOptions.difficultyIndex]))));
            }
        }
        Vector3 GetPositionFromRandomDirection(Vector3 centerDirection)
        {
            var playerFeetY = Player.local.transform.position.y;

            float maxAngle = 90f;
            float minDistance = 2f;
            float maxDistance = 5f;

            float angleRad = maxAngle * Mathf.Deg2Rad;
            float theta = UnityEngine.Random.Range(0, 2f * Mathf.PI);
            float phi = UnityEngine.Random.Range(0, angleRad);
            float distance = UnityEngine.Random.Range(minDistance, maxDistance);

            float x = Mathf.Sin(phi) * Mathf.Cos(theta);
            float y = Mathf.Abs(Mathf.Sin(phi) * Mathf.Sin(theta)); // <-- Force positive Y bias
            float z = Mathf.Cos(phi);

            Vector3 localDir = new Vector3(x, y, z).normalized;
            Vector3 localOffset = localDir * distance;

            Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, centerDirection.normalized);
            Vector3 worldOffset = rotation * localOffset;

            Vector3 finalPos = Player.local.creature.ragdoll.targetPart.transform.position + worldOffset;

            // Ensure it's always above the player's feet
            if (finalPos.y < playerFeetY + 0.5f)
                finalPos.y = playerFeetY + 0.5f;

            return finalPos;
        }
    }
}