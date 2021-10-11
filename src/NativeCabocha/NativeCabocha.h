// NativeCabocha.h

#pragma once

#include "cabocha.h"

using namespace System;

namespace NativeCabocha {

	ref class Token;

	public ref class Cabocha
	{
	private:
		cabocha_t*	m_Cabocha;

	public:
		Cabocha();
		array<Token^>^ Parse(String^ inputstr);
	};
}
