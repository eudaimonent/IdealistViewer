#include "main.h"

extern "C"
{
    EXPORT bool VideoDriver_BeginScene(IntPtr videodriver, bool backBuffer, bool zBuffer, M_SCOLOR color);
    EXPORT void VideoDriver_EndScene(IntPtr videodriver);
	EXPORT void VideoDriver_EndSceneA(IntPtr videodriver, int windowId, M_RECT viewRect); 
    EXPORT IntPtr VideoDriver_AddTexture(IntPtr videodriver, M_DIM2DS size, c8* name, ECOLOR_FORMAT fmt);
    EXPORT IntPtr VideoDriver_GetTexture(IntPtr videodriver, c8 *name);
	EXPORT IntPtr VideoDriver_GetGPUProgrammingServices(IntPtr videodriver);
    EXPORT int VideoDriver_GetFPS(IntPtr videodriver);
	EXPORT void VideoDriver_MakeColorKeyTexture(IntPtr videodriver, IntPtr texture, M_POS2DS colorKeyPixelPos);
	EXPORT void VideoDriver_MakeColorKeyTextureA(IntPtr videodriver, IntPtr texture, M_SCOLOR color);
	EXPORT void VideoDriver_MakeNormalMapTexture(IntPtr videodriver, IntPtr texture, float amplitude);
	EXPORT void VideoDriver_ClearZBuffer(IntPtr videodriver);
	EXPORT IntPtr VideoDriver_CreateImageFromFile(IntPtr videodriver, M_STRING filename);
	EXPORT IntPtr VideoDriver_AddTextureFromImage(IntPtr videodriver, c8 *name, IntPtr image); 
	EXPORT IntPtr VideoDriver_CreateRenderTargetTexture(IntPtr videodriver, M_DIM2DS size);
	EXPORT void VideoDriver_Draw2DImage(IntPtr videodriver, IntPtr texture, M_POS2DS destPos, M_RECT sourceRect, M_RECT clipRect, M_SCOLOR color, bool useAlphaChannelOfTexture);
	EXPORT void VideoDriver_Draw2DImageA(IntPtr videodriver, IntPtr texture, M_POS2DS destPos);
	EXPORT void VideoDriver_Draw2DImageB(IntPtr videodriver, IntPtr texture, M_POS2DS destPos, M_RECT sourceRect, M_SCOLOR color, bool useAlphaChannelOfTexture);
	EXPORT void VideoDriver_Draw2DImageC(IntPtr videodriver, IntPtr texture, M_RECT destPos, M_RECT sourceRect, M_RECT clipRect, M_SCOLOR color1, M_SCOLOR color2, M_SCOLOR color3, M_SCOLOR color4, bool useAlphaChannelOfTexture);
	EXPORT void VideoDriver_Draw2DImageD(IntPtr videodriver, IntPtr texture, M_RECT destPos, M_RECT sourceRect, M_SCOLOR color1, M_SCOLOR color2, M_SCOLOR color3, M_SCOLOR color4, bool useAlphaChannelOfTexture);
	EXPORT void VideoDriver_Draw2DLine(IntPtr videodriver, M_POS2DS start, M_POS2DS end, M_SCOLOR color);
	EXPORT void VideoDriver_Draw2DPolygon(IntPtr videodriver, M_POS2DS center, float radius, M_SCOLOR color, int vertexCount);
	EXPORT void VideoDriver_Draw2DRectangle(IntPtr videodriver, M_RECT pos, M_SCOLOR colorLeftUp, M_SCOLOR colorRightUp, M_SCOLOR colorLeftDown, M_SCOLOR colorRightDown);
	EXPORT void VideoDriver_Draw3DBox(IntPtr videodriver, M_BOX3D box, M_SCOLOR color);
	EXPORT void VideoDriver_Draw3DLine(IntPtr videodriver, M_VECT3DF start, M_VECT3DF end, M_SCOLOR color);
	EXPORT void VideoDriver_Draw3DTriangle(IntPtr videodriver, M_TRIANGLE3DF tri, M_SCOLOR color);
	EXPORT E_DRIVER_TYPE VideoDriver_GetDriverType(IntPtr videodriver);
	EXPORT void VideoDriver_GetScreenSize(IntPtr videodriver, M_DIM2DS size);
	EXPORT void VideoDriver_GetTransform(IntPtr videodriver, E_TRANSFORMATION_STATE state, M_MAT4 mat);
	EXPORT void VideoDriver_SetTransform(IntPtr videodriver, E_TRANSFORMATION_STATE state, M_MAT4 mat);
	EXPORT void VideoDriver_DrawIndexedTriangleList(IntPtr videodriver, IntPtr *vertices, int vertexCount, unsigned short *indexList, int triangleCount);
	EXPORT void VideoDriver_DrawIndexedTriangleListA(IntPtr videodriver, IntPtr *vertices, int vertexCount, unsigned short *indexList, int triangleCount);
	EXPORT void VideoDriver_DrawIndexedTriangleFan(IntPtr videodriver, IntPtr *vertices, int vertexCount, unsigned short *indexList, int triangleCount);
	EXPORT void VideoDriver_DrawIndexedTriangleFanA(IntPtr videodriver, IntPtr *vertices, int vertexCount, unsigned short *indexList, int triangleCount);
	EXPORT void VideoDriver_DrawVertexPrimitiveList(IntPtr videodriver, IntPtr *vertices, int vertexCount, unsigned short *indexList, int triangleCount, E_VERTEX_TYPE vType, E_PRIMITIVE_TYPE pType);
	EXPORT void VideoDriver_DrawMeshBuffer(IntPtr videodriver, IntPtr meshbuffer);
	EXPORT bool VideoDriver_GetTextureCreationFlag(IntPtr videodriver, E_TEXTURE_CREATION_FLAG flag);
	EXPORT void VideoDriver_GetViewPort(IntPtr videodriver, M_RECT viewport);
	EXPORT bool VideoDriver_QueryFeature(IntPtr videodriver, E_VIDEO_DRIVER_FEATURE feat);
	EXPORT void VideoDriver_RemoveAllTextures(IntPtr videodriver);
	EXPORT void VideoDriver_RemoveTexture(IntPtr videodriver, IntPtr texture);
	EXPORT void VideoDriver_SetAmbientLight(IntPtr videodriver, M_SCOLOR ambient);
	EXPORT void VideoDriver_SetFog(IntPtr videodriver, M_SCOLOR color, bool linear, float start, float end, float density, bool pixel, bool range);
	EXPORT void VideoDriver_SetMaterial(IntPtr videodriver, IntPtr material);
	EXPORT void VideoDriver_SetRenderTarget(IntPtr videodriver, IntPtr texture, bool cBB, bool cZB, M_SCOLOR color);
	EXPORT void VideoDriver_SetTextureFlag(IntPtr videodriver, E_TEXTURE_CREATION_FLAG flag, bool enabled);
	EXPORT void VideoDriver_SetViewPort(IntPtr videodriver, M_RECT viewport);
	EXPORT void VideoDriver_DeleteAllDynamicLights(IntPtr videodriver); 
	EXPORT IntPtr VideoDriver_CreateScreenshot(IntPtr videodriver);
	EXPORT void VideoDriver_WriteImageToFile(IntPtr videodriver, IntPtr image, M_STRING filename);
	EXPORT int VideoDriver_GetTextureCount(IntPtr videodriver);
	EXPORT IntPtr VideoDriver_GetTextureByIndex(IntPtr videodriver, int index);
	EXPORT int VideoDriver_GetPrimitiveCountDrawn(IntPtr videodriver);
}
