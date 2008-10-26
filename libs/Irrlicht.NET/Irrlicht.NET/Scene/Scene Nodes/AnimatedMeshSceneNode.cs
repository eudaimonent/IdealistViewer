using System;
using System.Runtime.InteropServices;
using System.Security;

namespace IrrlichtNETCP
{
	public enum MD2Animation
	{
		Stand,
		Run,
		Attack,
		Pain1,
		Pain2,
		Pain3,
		Jump,
		Flip,
		Salute,
		Fallback,
		Wave,
		Point,
		CrouchStand,
		CrouchWalk,
		CrouchAttack,
		CrouchPain,
		CrouchDeath,
		DeathFallback,
		DeathFallforward,
		DeathFallbackSlow,
		Boom,
		Count // Do not use
	}
	public delegate void AnimationEnd(AnimatedMeshSceneNode node);
	public class AnimatedMeshSceneNode : SceneNode
	{
		protected delegate void NativeAnimationEndDelegate(IntPtr node);
		protected void OnNativeAnimationEnd(IntPtr node)
		{
			if(AnimationEnd != null)
			{
				AnimatedMeshSceneNode anode =
					(AnimatedMeshSceneNode)NativeElement.GetObject(node, typeof(AnimatedMeshSceneNode));
				AnimationEnd(anode);
			}
		}
		public event AnimationEnd AnimationEnd;
		public AnimatedMeshSceneNode(IntPtr raw) : base(raw)
		{
            MainEventDelegate = OnNativeAnimationEnd;
            AnimatedMeshSceneNode_SetAnimationEndCallback(_raw, MainEventDelegate);
		}
        /// <summary>
        /// This delegate must never been collected by GC and that's why we
        /// need to create this object.
        /// </summary>
        NativeAnimationEndDelegate MainEventDelegate;
		
		public int CurrentFrame
		{
			get
			{
				return AnimatedMeshSceneNode_GetFrameNr(_raw);
			}
			set
			{
				AnimatedMeshSceneNode_SetCurrentFrame(_raw, value);
			}
		}
		
		public int AnimationSpeed
		{
			set
			{
				AnimatedMeshSceneNode_SetAnimationSpeed(_raw, value);
			}
		}
		
		public bool LoopMode
		{
			set
			{
				AnimatedMeshSceneNode_SetLoopMode(_raw, value);
			}
		}
		
		public AnimatedMesh AnimatedMesh
		{
			get
			{
				return (AnimatedMesh)
					NativeElement.GetObject(AnimatedMeshSceneNode_GetMesh(_raw),
											typeof(AnimatedMesh));
			}
		}
		
		
        /// <summary>
        /// Adds a shadow to the node
        /// </summary>
        /// <param name="ID">ID of the shadow, I advice -1 for automatic assignation</param>
        /// <param name="zfail">Shall we use zfail ? (in fact it is not really implemented but I advice "true")</param>
        /// <param name="infinity">Infinity, I advice 10000f</param>
        /// <returns></returns>
		public ShadowVolumeSceneNode AddShadowVolumeSceneNode(int ID,bool zfail, float infinity)
		{
			return (ShadowVolumeSceneNode)
				NativeElement.GetObject(AnimatedMeshSceneNode_AddShadowVolumeSceneNode(_raw, ID, zfail, infinity),
										typeof(ShadowVolumeSceneNode));
		}
		
		/// <summary>
		/// Retrieves the MS3D Joint node whose name is [jointName]
		/// </summary>
		/// <returns>A SceneNode</returns>
		/// <param name="jointName">Name of the node</param>
		public SceneNode GetMS3DJointNode(string jointName)
		{
			return (SceneNode)
				NativeElement.GetObject(AnimatedMeshSceneNode_GetMS3DJointNode(_raw, jointName),
										typeof(SceneNode));
		}
		
		/// <summary>
		/// Retrieves the X Joint node whose name is [jointName]
		/// </summary>
		/// <returns>A SceneNode</returns>
		/// <param name="jointName">Name of the node</param>
		public SceneNode GetXJointNode(string jointName)
		{
			return (SceneNode)
				NativeElement.GetObject(AnimatedMeshSceneNode_GetXJointNode(_raw, jointName),
										typeof(SceneNode));
		}
		
		/// <summary>
		/// Sets the animation loop between these two frames
		/// </summary>
		/// <param name="start">Starting frame</param>
		/// <param name="end">Ending frame</param>
		public void SetFrameLoop(int start, int end)
		{
			AnimatedMeshSceneNode_SetFrameLoop(_raw, start, end);
		}
		
		/// <summary>
		/// Sets the current MD2 animation
		/// </summary>
		/// <param name="animationname">Animation name (please refer to Irrlicht's documentation)</param>
		public void SetMD2Animation(string animationname)
		{
			AnimatedMeshSceneNode_SetMD2Animation(_raw, animationname);
		}
		
		/// <summary>
		/// Sets the current MD2 animation
		/// </summary>
		/// <param name="anim">Animation</param>
		public void SetMD2Animation(MD2Animation anim)
		{
			AnimatedMeshSceneNode_SetMD2AnimationA(_raw, anim);
		}
	
		#region Native Invokes
		 [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
		static extern IntPtr AnimatedMeshSceneNode_AddShadowVolumeSceneNode(IntPtr node, int ID, bool zfail, float infinity);
		
		 [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
		static extern int AnimatedMeshSceneNode_GetFrameNr(IntPtr node);
		
		 [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
		static extern IntPtr AnimatedMeshSceneNode_GetMS3DJointNode(IntPtr node, string jointName);
		
		 [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
		static extern IntPtr AnimatedMeshSceneNode_GetXJointNode(IntPtr node, string jointName);
		
		 [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
		static extern void AnimatedMeshSceneNode_SetAnimationEndCallback(IntPtr node, NativeAnimationEndDelegate callback);
		
		 [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
		static extern void AnimatedMeshSceneNode_SetAnimationSpeed(IntPtr node, int framePS);
		
		 [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
		static extern void AnimatedMeshSceneNode_SetCurrentFrame(IntPtr node, int cf);
		
		 [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
		static extern void AnimatedMeshSceneNode_SetFrameLoop(IntPtr node, int start, int end);
		
		 [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
		static extern void AnimatedMeshSceneNode_SetLoopMode(IntPtr node, bool animationLooped);
		
		 [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
		static extern void AnimatedMeshSceneNode_SetMD2Animation(IntPtr node, string animationname);
		
		 [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
		static extern void AnimatedMeshSceneNode_SetMD2AnimationA(IntPtr node, MD2Animation anim);
		 
		 [DllImport(Native.Dll), SuppressUnmanagedCodeSecurity]
		static extern IntPtr AnimatedMeshSceneNode_GetMesh(IntPtr node);
		
		#endregion
	}
	
}
