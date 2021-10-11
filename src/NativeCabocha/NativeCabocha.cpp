#include <stdio.h>
#include <stdlib.h>

#include "NativeCabocha.h"
#include "cabocha.h"
using namespace System::Runtime::InteropServices;

extern int run();

namespace NativeCabocha {

Cabocha::Cabocha()
{
	m_Cabocha = cabocha_new(0, NULL);
}

array<Token^>^ Cabocha::Parse(String^ inputstr)
{
	if (m_Cabocha == 0)
	{
		throw gcnew Exception("cabocha_t is null.");
	}
	IntPtr ip = Marshal::StringToHGlobalAnsi(inputstr);
	const char* p = static_cast<const char*>(ip.ToPointer());

	cabocha_tree_t* tree = const_cast<cabocha_tree_t*>(cabocha_sparse_totree(m_Cabocha, p));
	Marshal::FreeHGlobal( ip );

	if (tree == 0)
	{
		throw gcnew Exception("cabocha returns null tree.");
	}
	unsigned int size = cabocha_tree_token_size(tree);
	array<Token^>^ ret = gcnew array<Token^>(size);

	for (unsigned int i = 0; i < size; ++i)
	{
		const cabocha_token_t* source = cabocha_tree_token(tree, i);
		Token^ target = gcnew Token();
		if (source->chunk != NULL)
		{
			Chunk^ chunk = gcnew Chunk();
			chunk->Link = source->chunk->link;
			chunk->HeadPos = source->chunk->head_pos;
			chunk->FuncPos = source->chunk->func_pos;
			chunk->Score = source->chunk->score;
			target->Chunk = chunk;
		}
		//target->Surface = gcnew System::String(source->surface);  // NG for VC90
		target->Surface = Marshal::PtrToStringAnsi(static_cast<IntPtr>((char*)source->surface));
		target->Feature = Marshal::PtrToStringAnsi(static_cast<IntPtr>((char*)source->feature));
		target->Ne = (source->ne)?
			Marshal::PtrToStringAnsi(static_cast<IntPtr>((char*)source->ne)) : "O";
		ret[i] = target;
	}
	return ret;

}

}
 