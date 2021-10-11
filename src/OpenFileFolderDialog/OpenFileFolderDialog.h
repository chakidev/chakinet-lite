// OpenFileFolderDialog.h

#pragma once

#include <windows.h>
#include <atlbase.h>
#include <atlapp.h>
#include <atldlgs.h>
#include <shtypes.h>
#include <msclr\marshal.h>

using namespace System;
using namespace System::Runtime::InteropServices;
using namespace System::Windows::Forms;

namespace ChaKi {

	// For pre-XP: Common Dialog Callback
	// Not intended to be used under multi-thread.
	static OPENFILENAME  OpenFileName;

	static UINT_PTR CALLBACK OFNHookProc(HWND hdlg, UINT uiMsg, WPARAM wParam, LPARAM lParam )
	{
		HWND hwnd_dlg = GetParent(hdlg);
		if (hwnd_dlg == NULL) {
			return 0;
		}

		if (uiMsg == WM_INITDIALOG) {
			// 「開く」ボタンの文字を「OK」に変更
			HWND hwnd_ok = GetDlgItem(hwnd_dlg, IDOK);
			if (hwnd_ok == NULL) {
				return 0;
			}
			SetWindowText(hwnd_ok, L"OK");
		}
		if (uiMsg == WM_NOTIFY) {
			if (((LPNMHDR)lParam)->code == CDN_FOLDERCHANGE) {
				wchar_t strPath[MAX_PATH];
				ZeroMemory(strPath, sizeof(strPath));
				SendMessage( hwnd_dlg, CDM_GETFOLDERPATH, (WPARAM)MAX_PATH, (LPARAM)strPath);
				wcsncpy( OpenFileName.lpstrFile, (LPCWSTR)strPath, OpenFileName.nMaxFile );

				// マウスポインタが「開く」ボタンの上にあるか
				HWND hwnd_ok = GetDlgItem(hwnd_dlg, IDOK);
				if (hwnd_ok == NULL) {
					return 0;
				}
				RECT rect;
				POINT cpos;
				GetCursorPos( &cpos );
				GetWindowRect( hwnd_ok, &rect );
				if (cpos.x >= rect.left && cpos.x <= rect.right
					&& cpos.y >= rect.top && cpos.y <= rect.bottom) {
					EndDialog(hwnd_dlg, IDOK);
				}
			}
		}
		return 0;
	}

	// For post-Vista: using WTL and ShellFileOpenDialog
	class OpenDialogForVista : public CShellFileOpenDialogImpl<OpenDialogForVista>
	{
	public:
		UINT FileTypeIndexForFolder;

	private:
		bool m_SwitchToFolderDialog;

	public:
		OpenDialogForVista()
		{
			m_SwitchToFolderDialog = false;
			FileTypeIndexForFolder = 0;
		}

		int ShowDialog()
		{
			int ret = DoModal();
			if (m_SwitchToFolderDialog)
			{
				m_spFileDlg.Release();
				m_spFileDlg.CoCreateInstance(CLSID_FileOpenDialog);
				DWORD opt;
				m_spFileDlg->GetOptions( &opt );
				m_spFileDlg->SetOptions( opt|FOS_PICKFOLDERS );
				ret = DoModal();
			}
			return ret;
		}

		HRESULT OnTypeChange()
		{
			UINT index = 0;
			m_spFileDlg->GetFileTypeIndex( &index );
			if (index == FileTypeIndexForFolder) {
				m_SwitchToFolderDialog = true;
				m_spFileDlg->Close(IDOK);
			}
			return 0;
		}

#if 0 //2009.12.14: デバッグ中：File選択でもFolder名しか得られていない。
		HRESULT OnFolderChange()
		{
			ATL::CComPtr<IShellItem> spItem;
			if (GetPtr()->GetFolder( &spItem ) != S_OK) {
				return 0;
			}
			LPWSTR lpstrName = NULL;
			spItem->GetDisplayName( SIGDN_FILESYSPATH, &lpstrName );
			GetPtr()->SetFileName( lpstrName );
			CoTaskMemFree( lpstrName );

			HWND hwnd_dlg = ::GetActiveWindow();
			// マウスポインタが「開く」ボタンの上にあるか
			HWND hwnd_ok = GetDlgItem(hwnd_dlg, IDOK);
			if (hwnd_ok == NULL) {
				return 0;
			}
			RECT rect;
			POINT cpos;
			GetCursorPos( &cpos );
			GetWindowRect( hwnd_ok, &rect );
			if (cpos.x >= rect.left && cpos.x <= rect.right
				&& cpos.y >= rect.top && cpos.y <= rect.bottom) {
				EndDialog(hwnd_dlg, IDOK);
			}
			return 0;
		}
#endif
	};

	public ref class OpenFileFolderDialog
	{
	public:
		bool m_bVistaStyle;
		String^ FileName;
		String^ DefExt;
		String^ Title;
		String^ FilterSpec;
		Boolean FileMustExist;
		Int32 FilterIndex;

	public:
		OpenFileFolderDialog()
		{
			FileName = String::Empty;
			DefExt = String::Empty;
			Title = String::Empty;
			FilterSpec = String::Empty;
			FileMustExist = false;
			FilterIndex = 1;

			OSVERSIONINFO vi;
			ZeroMemory(&vi, sizeof(OSVERSIONINFO));
			vi.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);
			::GetVersionEx(&vi);
			// if running under Vista
			m_bVistaStyle = (vi.dwMajorVersion >= 6);
		}

		DialogResult DoModal()
		{
			if (m_bVistaStyle) {
				return DoModal_Vista();
			}
			return DoModal_XP();
		}

		
		DialogResult DoModal_XP()
		{
		    wchar_t    szFile[MAX_PATH] = L"";

			ZeroMemory( &OpenFileName, sizeof(OpenFileName) );
			OpenFileName.lStructSize     = sizeof(OpenFileName);
			OpenFileName.lpstrFile       = szFile;
			OpenFileName.nMaxFile        = MAX_PATH;
			LPWSTR s = (LPWSTR)(void*)Marshal::StringToHGlobalUni(FilterSpec + L"||");
			int l = wcslen(s);
			for (int i = 0; i < l; i++) {
				if (s[i] == L'|') s[i] = L'\0';
			}
			OpenFileName.lpstrFilter     = s;
			OpenFileName.lpstrTitle      = (LPCWSTR)(void*)Marshal::StringToHGlobalUni(Title);
			OpenFileName.lpfnHook = OFNHookProc;
			OpenFileName.Flags |= (OFN_ENABLEHOOK|OFN_EXPLORER|OFN_ENABLESIZING);
			OpenFileName.Flags |= OFN_PATHMUSTEXIST;
			LPCWSTR ext = (LPCWSTR)Marshal::StringToHGlobalUni(DefExt).ToPointer();
			OpenFileName.lpstrDefExt = ext;
			OpenFileName.nFilterIndex = FilterIndex;
			if (FileMustExist)
			{
				OpenFileName.Flags |= OFN_FILEMUSTEXIST;
			}

		    BOOL ret = GetOpenFileName(&OpenFileName);
			Marshal::FreeHGlobal((IntPtr)(void*)ext);
			if (ret) {
				this->FileName = Marshal::PtrToStringUni((IntPtr)szFile);
				return DialogResult::OK;
			}
			return DialogResult::Cancel;
		}

		DialogResult DoModal_Vista()
		{
			OpenDialogForVista dlg;

			DWORD opts = FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST;
			if (FileMustExist)
			{
				opts |= FOS_FILEMUSTEXIST;
			}
			dlg.GetPtr()->SetOptions(opts);
			COMDLG_FILTERSPEC* filterSpec;
			if (FilterSpec != nullptr) {
				array<String^>^ tags = FilterSpec->Split(gcnew array<Char>(1) {'|'});
				if (tags->Length > 1 && tags->Length%2 == 0) {
					int count = tags->Length/2;
					filterSpec = new COMDLG_FILTERSPEC[count];
					for (int i = 0; i < count; i++) {
						filterSpec[i].pszName = (LPCWSTR)(void*)Marshal::StringToHGlobalUni(tags[i*2]);
						filterSpec[i].pszSpec = (LPCWSTR)(void*)Marshal::StringToHGlobalUni(tags[i*2+1]);
					}
					dlg.GetPtr()->SetFileTypes( count, filterSpec );
					dlg.FileTypeIndexForFolder = count;
				}
			}
			if (Title != nullptr || Title->Length > 0) {
				dlg.GetPtr()->SetTitle( (LPCWSTR)(void*)Marshal::StringToHGlobalUni(Title) );
			}
			LPCWSTR ext = (LPCWSTR)Marshal::StringToHGlobalAnsi(DefExt).ToPointer();
			dlg.GetPtr()->SetDefaultExtension( ext );
			dlg.GetPtr()->SetOkButtonLabel( L"OK" );
			dlg.GetPtr()->SetFileTypeIndex( FilterIndex );

			int ret = dlg.ShowDialog();
			Marshal::FreeHGlobal((IntPtr)(void*)ext);
			if (ret == IDOK) {
				ATL::CComPtr<IShellItem> spItem;
				dlg.GetPtr()->GetResult( &spItem );
				LPWSTR lpstrName = NULL;
				spItem->GetDisplayName( SIGDN_FILESYSPATH, &lpstrName );
				this->FileName = Marshal::PtrToStringUni((IntPtr)lpstrName);
				UINT fidx;
				dlg.GetPtr()->GetFileTypeIndex( &fidx );
				this->FilterIndex = fidx;

				return DialogResult::OK;
			}
			return DialogResult::Cancel;
		}
	};
}
