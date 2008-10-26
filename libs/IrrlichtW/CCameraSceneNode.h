// Copyright (C) 2002-2007 Nikolaus Gebhardt
// This file is part of the "Irrlicht Engine".
// For conditions of distribution and use, see copyright notice in irrlicht.h

#ifndef __C_CAMERA_SCENE_NODE_H_INCLUDED__
#define __C_CAMERA_SCENE_NODE_H_INCLUDED__

#include "ICameraSceneNode.h"
#include "SViewFrustum.h"

namespace irr
{
namespace scene
{

	class CCameraSceneNode : public ICameraSceneNode
	{
	public:

		//! constructor
		CCameraSceneNode(ISceneNode* parent, ISceneManager* mgr, s32 id, 
			const core::vector3df& position = core::vector3df(0,0,0),
			const core::vector3df& lookat = core::vector3df(0,0,100));

		//! destructor
		virtual ~CCameraSceneNode();

		//! Sets the projection matrix of the camera. The core::matrix4 class has some methods
		//! to build a projection matrix. e.g: core::matrix4::buildProjectionMatrixPerspectiveFovLH
		//! \param projection: The new projection matrix of the camera. 
		virtual void setProjectionMatrix(const core::matrix4& projection);

		//! Gets the current projection matrix of the camera
		//! \return Returns the current projection matrix of the camera.
		virtual const core::matrix4& getProjectionMatrix();

		//! Gets the current view matrix of the camera
		//! \return Returns the current view matrix of the camera.
		virtual const core::matrix4& getViewMatrix();

		//! It is possible to send mouse and key events to the camera. Most cameras
		//! may ignore this input, but camera scene nodes which are created for 
		//! example with scene::ISceneManager::addMayaCameraSceneNode or
		//! scene::ISceneManager::addMeshViewerCameraSceneNode, may want to get this input
		//! for changing their position, look at target or whatever. 
		virtual bool OnEvent(SEvent event);

		//! sets the look at target of the camera
		//! \param pos: Look at target of the camera.
		virtual void setTarget(const core::vector3df& pos);

		//! Gets the current look at target of the camera
		//! \return Returns the current look at target of the camera
		virtual core::vector3df getTarget() const;

		//! Sets the up vector of the camera.
		//! \param pos: New upvector of the camera.
		virtual void setUpVector(const core::vector3df& pos);

		//! Gets the up vector of the camera.
		//! \return Returns the up vector of the camera.
		virtual core::vector3df getUpVector() const;

		//! Gets distance from the camera to the near plane.
		//! \return Value of the near plane of the camera.
		virtual f32 getNearValue();

		//! Gets the distance from the camera to the far plane.
		//! \return Value of the far plane of the camera.
		virtual f32 getFarValue();

		//! Get the aspect ratio of the camera.
		//! \return The aspect ratio of the camera.
		virtual f32 getAspectRatio();

		//! Gets the field of view of the camera.
		//! \return Field of view of the camera
		virtual f32 getFOV();

		//! Sets the value of the near clipping plane. (default: 1.0f)
		virtual void setNearValue(f32 zn);

		//! Sets the value of the far clipping plane (default: 2000.0f)
		virtual void setFarValue(f32 zf);

		//! Sets the aspect ratio (default: 4.0f / 3.0f)
		virtual void setAspectRatio(f32 aspect);

		//! Sets the field of view (Default: PI / 3.5f)
		virtual void setFOV(f32 fovy);

		//! PreRender event
		virtual void OnRegisterSceneNode();

		//! Render
		virtual void render();

		//! Returns the axis aligned bounding box of this node
		virtual const core::aabbox3d<f32>& getBoundingBox() const;

		//! Returns the view area. Sometimes needed by bsp or lod render nodes.
		virtual const SViewFrustum* getViewFrustum() const;

		//! Disables or enables the camera to get key or mouse inputs.
		//! If this is set to true, the camera will respond to key inputs
		//! otherwise not.
		virtual void setInputReceiverEnabled(bool enabled);

		//! Returns if the input receiver of the camera is currently enabled.
		virtual bool isInputReceiverEnabled();

		//! Writes attributes of the scene node.
		virtual void serializeAttributes(io::IAttributes* out, io::SAttributeReadWriteOptions* options=0);

		//! Reads attributes of the scene node.
		virtual void deserializeAttributes(io::IAttributes* in, io::SAttributeReadWriteOptions* options=0);

		//! Returns type of the scene node
		virtual ESCENE_NODE_TYPE getType() const { return ESNT_CAMERA; }

		virtual core::vector3df getAbsolutePosition() const;

	protected:

		void recalculateProjectionMatrix();
		void recalculateViewArea();

		core::vector3df Target;
		core::vector3df UpVector;

		f32 Fovy;	// Field of view, in radians. 
		f32 Aspect;	// Aspect ratio. 
		f32 ZNear;	// value of the near view-plane. 
		f32 ZFar;	// Z-value of the far view-plane.

		SViewFrustum ViewArea;

		bool InputReceiverEnabled;
	};

} // end namespace
} // end namespace

#endif

