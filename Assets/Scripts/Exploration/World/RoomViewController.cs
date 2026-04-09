using System.Collections.Generic;
using Exploration.Camera;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

// World 네임스페이스
namespace Exploration.World
{
    /// <summary>
    /// 플레이어가 머문 방에 맞춰 카메라 bounds와 가림 오브젝트를 함께 전환한다.
    /// </summary>
    [MovedFrom(false, sourceNamespace: "World", sourceAssembly: "Assembly-CSharp", sourceClassName: "RoomViewController")]
    public class RoomViewController : MonoBehaviour
    {
        [SerializeField] private CameraFollow cameraFollow;

        private readonly HashSet<RoomViewZone> occupiedZones = new();
        private RoomViewZone currentZone;

        private void Awake()
        {
            if (cameraFollow == null)
            {
                cameraFollow = FindFirstObjectByType<CameraFollow>();
            }
        }

        /// <summary>
        /// 빌더와 씬 설정 코드에서 카메라 참조를 다시 연결한다.
        /// </summary>
        public void Configure(CameraFollow follow)
        {
            cameraFollow = follow;
        }

        public void NotifyZoneEntered(RoomViewZone zone)
        {
            if (zone == null)
            {
                return;
            }

            occupiedZones.Add(zone);
            RefreshCurrentZone(true);
        }

        public void NotifyZoneExited(RoomViewZone zone)
        {
            if (zone == null)
            {
                return;
            }

            occupiedZones.Remove(zone);
            RefreshCurrentZone(true);
        }

        public void NotifyZoneDisabled(RoomViewZone zone)
        {
            if (zone == null)
            {
                return;
            }

            occupiedZones.Remove(zone);
            if (currentZone == zone)
            {
                zone.ApplyPresentation(false);
                currentZone = null;
            }

            RefreshCurrentZone(false);
        }

        private void RefreshCurrentZone(bool snapImmediately)
        {
            RoomViewZone nextZone = ResolveHighestPriorityZone();
            if (currentZone == nextZone)
            {
                if (nextZone != null)
                {
                    ApplyZone(nextZone, snapImmediately);
                }

                return;
            }

            if (currentZone != null)
            {
                currentZone.ApplyPresentation(false);
            }

            currentZone = nextZone;

            if (currentZone != null)
            {
                currentZone.ApplyPresentation(true);
                ApplyZone(currentZone, snapImmediately);
                return;
            }

            if (cameraFollow != null)
            {
                cameraFollow.ClearBoundsOverride(snapImmediately);
                cameraFollow.ClearOrthographicSizeOverride(snapImmediately);
            }
        }

        private void ApplyZone(RoomViewZone zone, bool snapImmediately)
        {
            if (zone == null || cameraFollow == null)
            {
                return;
            }

            cameraFollow.SetBoundsOverride(zone.CameraBounds, snapImmediately && zone.SnapCameraOnEnter);

            if (zone.CameraOrthographicSize > 0.01f)
            {
                cameraFollow.SetOrthographicSizeOverride(zone.CameraOrthographicSize, snapImmediately && zone.SnapCameraOnEnter);
            }
            else
            {
                cameraFollow.ClearOrthographicSizeOverride(snapImmediately && zone.SnapCameraOnEnter);
            }
        }

        private RoomViewZone ResolveHighestPriorityZone()
        {
            RoomViewZone bestZone = null;
            foreach (RoomViewZone zone in occupiedZones)
            {
                if (zone == null || !zone.isActiveAndEnabled || !zone.IsOccupied)
                {
                    continue;
                }

                if (bestZone == null || zone.Priority > bestZone.Priority)
                {
                    bestZone = zone;
                }
            }

            return bestZone;
        }
    }
}
