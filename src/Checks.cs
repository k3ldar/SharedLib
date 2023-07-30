/*
 *  The contents of this file are subject to MIT Licence.  Please
 *  view License.txt for further details. 
 *
 *  The Original Code was created by Simon Carter (s1cart3r@gmail.com)
 *
 *  Copyright (c) 2011 Simon Carter
 *
 *  Purpose:  Provide warning depending on how the library is being built
 *
 */

namespace Shared
{
#if FAKE_ADDRESS
#warning Remove FAKE_ADDRESS compiler directive for release build of Shared.dll
#endif

#if DEBUG
#warning Debug Build Shared.dll
#endif

#if TRACE
#warning Trace enabled for Shared.dll
#endif
}
