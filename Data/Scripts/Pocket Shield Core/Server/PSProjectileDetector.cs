// ;
using ExShared;
using Sandbox.ModAPI;
using System;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace PocketShieldCore
{
    internal class PSProjectileDetector : IMyProjectileDetector
    {
        public static MyEntity DummyEntity = null;

        public bool IsDetectorEnabled { get; private set; } = true;
        public IMyEntity HitEntity { get; private set; } = null;
        public BoundingBoxD DetectorAABB { get; private set; }

        public Logger m_Logger = null;

        private BoundingSphereD m_Sphere;

        private readonly CharacterShieldInfo m_Parent = null;

        static PSProjectileDetector()
        {
            DummyEntity = new MyEntity()
            {
                Save = false,
                SyncFlag = false,
                IsPreview = true,
                NeedsWorldMatrix = false,
                NeedsUpdate = MyEntityUpdateEnum.NONE,
            };
            DummyEntity.Init(null, null, null, null);

            DummyEntity.Render.CastShadows = false;
            DummyEntity.Flags &= ~EntityFlags.IsGamePrunningStructureObject;
            DummyEntity.Flags |= ~EntityFlags.IsNotGamePrunningStructureObject;

            //MyAPIGateway.Entities.AddEntity(DummyEntity, false);
            DummyEntity.RemoveFromGamePruningStructure();
        }

        public PSProjectileDetector(CharacterShieldInfo _parent, float _radius)
        {
            m_Parent = _parent;
            
            //m_Sphere = new BoundingSphereD(m_Parent.Position, _radius);
            //DetectorAABB = new BoundingBoxD(m_Parent.Position - new Vector3D(_radius), m_Parent.Position + new Vector3D(_radius));
            
        }

        public bool GetDetectorIntersectionWithLine(ref LineD _line, out Vector3D? _intersectPoint)
        {
            if (!IsDetectorEnabled)
            {
                _intersectPoint = null;
                return false;
            }

            // TODO: implement this;
            RayD ray = new RayD(_line.From, _line.Direction);


            double? distance = ray.Intersects(m_Sphere);

            if (distance.HasValue && distance.Value <= _line.Length)
            {
                _intersectPoint = _line.From + _line.Direction * distance.Value;
                MyAPIGateway.Utilities.ShowNotification("Intersection: HIT at " + _intersectPoint);
                //HitEntity = m_Parent.Character;
                HitEntity = DummyEntity;
                return true;
            }
            else
            {
            MyAPIGateway.Utilities.ShowNotification("Intersection: MISS");
                _intersectPoint = null;
                return false;
            }


            // check manual shield;
            //m_Parent.Character.WorldMatrix.GetOrientation


            // check auto shield;

        }

        public void Update()
        {
            IsDetectorEnabled = m_Parent.HasAnyEmitter;
            IsDetectorEnabled = false;
            if (!IsDetectorEnabled)
            {
                return;
            }

            //m_Sphere.Center = m_Parent.Position;
            MyAPIGateway.Projectiles.RemoveHitDetector(this);
            DetectorAABB = new BoundingBoxD(m_Sphere.Center - new Vector3D(m_Sphere.Radius), m_Sphere.Center + new Vector3D(m_Sphere.Radius));
            MyAPIGateway.Projectiles.AddHitDetector(this);


        }
    }
}



/*
 *     public class HitDetector : IMyProjectileDetector
    {
        public bool IsDetectorEnabled { get; } = true; // to prevent it from being called without needing to register (which'll be slower)
        public IMyEntity HitEntity { get; private set; }
        public BoundingBoxD DetectorAABB { get; private set; }

        public BoundingSphereD Sphere;

        public HitDetector(Vector3D position, float radius)
        {
            Sphere = new BoundingSphereD(position, radius);
            DetectorAABB = new BoundingBoxD(position - new Vector3D(radius), position + new Vector3D(radius));
        }

        public bool GetDetectorIntersectionWithLine(ref LineD line, out Vector3D? hit)
        {
            RayD ray = new RayD(ref line.From, ref line.Direction);
            double? hitDistance = ray.Intersects(Sphere);

            //MyAPIGateway.Utilities.ShowNotification($"{GetType().Name}.GetDetectorIntersectionWithLine() :: line={line.Length:0.##}m hitDistance={hitDistance:0.##}", 16, MyFontEnum.Debug);

            if(hitDistance.HasValue && hitDistance.Value <= line.Length)
            {
                MyAPIGateway.Utilities.ShowNotification("hit detector!", 2000, MyFontEnum.Debug);

                hit = line.From + line.Direction * hitDistance.Value;
                return true;
            }
            else
            {
                hit = null;
                return false;
            }
        }
    }
*/