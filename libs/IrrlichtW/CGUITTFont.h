#if COMPILE_WITH_FREETYPE || !WIN32

#ifndef __C_GUI_TTFONT_H_INCLUDED__
#define __C_GUI_TTFONT_H_INCLUDED__
#ifdef WIN32
#pragma comment(lib, "\"..\\Irrlicht SDK\\lib\\Win32-visualstudio\\freetype2110MT.lib\"")
#endif

//If on Windows you get an error here, add to your IDE's incldue path
//"%Irrlicht .NET CP SDK directory%"/Irrlicht SDK/include"
//Or remove from the preprocessor's configuration COMPILE_WITH_FREETYPE
#include <ft2build.h>
#include <freetype/freetype.h>
// >> Add by uirou for multibyte language start
#ifdef USE_ICONV
#include <langinfo.h>
#include <iconv.h>
#endif
// << Add by uirou for multibyte language end

namespace irr
{
namespace gui
{
class CGUITTFace : public IUnknown
{
public:
	CGUITTFace();
	virtual ~CGUITTFace();
	bool loaded;
	FT_Library	library;
	FT_Face		face;
// >> Add by uirou for multibyte language start
#ifdef USE_ICONV
	iconv_t cd;
#endif
// << Add by uirou for multibyte language end
	bool load(const c8* filename);
};
class CGUITTGlyph : public IUnknown
{
public:
	bool cached;
	video::IVideoDriver* Driver;
	CGUITTGlyph();
	virtual ~CGUITTGlyph();
// >> Add solehome's code for memory access error begin
	void init();
// << Add solehome's code for memory access error end
	void cache(u32 idx);
	FT_Face *face;
	u32 size;
	u32 top;
	u32 left;
	u32 texw;
	u32 texh;
	u32 imgw;
	u32 imgh;
	video::ITexture *tex;
	u32 top16;
	u32 left16;
	u32 texw16;
	u32 texh16;
	u32 imgw16;
	u32 imgh16;
	video::ITexture *tex16;
	s32 offset;
	u8 *image;
};
class CGUITTFont : public IGUIFont
{
public:
	u32 size;

	//! constructor
	CGUITTFont(video::IVideoDriver* Driver);

	//! destructor
	virtual ~CGUITTFont();

	//! loads a truetype font file
	bool attach(CGUITTFace *Face,u32 size);

	//! draws an text and clips it to the specified rectangle if wanted
	virtual void draw(const wchar_t* text, const core::rect<s32>& position, video::SColor color, bool hcenter=false, bool vcenter=false, const core::rect<s32>* clip=0);

	//! returns the dimension of a text
	virtual core::dimension2d<s32> getDimension(const wchar_t* text);

	//! Calculates the index of the character in the text which is on a specific position.
	virtual s32 getCharacterFromPos(const wchar_t* text, s32 pixel_x);

// >> Add for Ver.1.3 begin
	//! set an Pixel Offset on Drawing ( scale position on width )
	virtual void setKerningWidth (s32 kerning);
	virtual void setKerningHeight (s32 kerning);

	//! set an Pixel Offset on Drawing ( scale position on width )
	virtual s32 getKerningWidth(const wchar_t* thisLetter=0, const wchar_t* previousLetter=0);
	virtual s32 getKerningHeight();
// << Add for Ver.1.3 end

	scene::ISceneNode *createBillboard(const wchar_t* text,scene::ISceneManager *scene,scene::ISceneNode *parent = 0,s32 id = -1);

	bool AntiAlias;
	bool TransParency;
	bool attached;
private:
	s32 getWidthFromCharacter(wchar_t c);
	u32 getGlyphByChar(wchar_t c);
	video::IVideoDriver* Driver;
	core::array< CGUITTGlyph > Glyphs;
	CGUITTFace *tt_face;
// >> Add for Ver.1.3 begin
	s32 GlobalKerningWidth, GlobalKerningHeight;
// << Add for Ver.1.3 end
};

} // end namespace gui
} // end namespace irr

#endif


#endif
