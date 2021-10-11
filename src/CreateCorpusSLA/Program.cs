using System;
using System.IO;

namespace CreateCorpusSLA
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateCorpus cc = new CreateCorpus();

            Console.WriteLine("args={0}\n", string.Join(",", args));

            if (!cc.ParseArguments(args))
            {
                PrintUsage();
                goto Exit;
            }
            if (cc.CreateSeparateDB)
            {
                // �^�[�Q�b�g������DB�ł���ꍇ�́A���̓t�H���_�̊e�t�@�C���ɂ��ă��[�v
                // SQLite�̏ꍇ:
                //   �o�͂Ƃ��ăt�H���_�p�X���w�肵���ꍇ�ɂ����ɗ���.
                //   �o�͂́A�w�肳�ꂽ�t�H���_�̉��A���̓t�@�C���̃t�@�C��������+".db"�Ƃ������O�̃t�@�C��
                // MySQL���̏ꍇ:
                //   "Separate Database"�`�F�b�N�{�b�N�X��ON�ɂ����ꍇ�ɂ����ɗ���.
                //   �o�͂́A���̓t�@�C���̃t�@�C���������Ɠ������O�̃f�[�^�x�[�X
                string ipath = cc.InputPath;
                if (!CreateCorpus.PathIsFolder(ipath))
                {
                    Console.WriteLine("Input Path must be a folder name when -p is set.");
                    goto Exit;
                }
                string opath = cc.CorpusName;
                string folder = string.Empty;
                if (CreateCorpus.PathIsFolder(opath))
                {
                    folder = opath;
                }
                else
                {
                    folder = Path.GetDirectoryName(opath);
                }
                string[] files = Directory.GetFileSystemEntries(ipath);
                foreach (string s in files)
                {
                    if (CreateCorpus.PathIsFolder(s)) continue;

                    cc.InputPath = s;
                    cc.CorpusName = Path.GetFileNameWithoutExtension(s);
                    cc.ForceCheckInputExtension = true;
                    if (cc.DbType == "SQLite")
                    {
                        // �t�H���_�Ɗg���q��ǉ�
                        cc.CorpusName = string.Format("{0}\\{1}.db", folder, cc.CorpusName);
                    }
                    DoJobs(cc);
                }
            }
            else if (cc.ReaderType == "Mecab|Cabocha|UniDic2|LUW")
            {
                // LUW(���P��)�A�m�e�[�V�����t����Cabocha�t�@�C���̏���
                // Proj 0�ɒʏ��Cabocha�C���|�[�g���s������A
                // ���P�ʃA�m�e�[�V������Proj 1�ɒǉ�����Q�i�K�̃C���|�[�g�����ɂȂ�.
                DoLUWJobs(cc);
            }
            else
            {
                DoJobs(cc);
            }
            if (cc.DoNotPauseOnExit)
            {
                return;
            }
        Exit:
            Console.WriteLine("\nPress Enter to exit.");
            Console.ReadLine();
        }

        static private void DoJobs(CreateCorpus cc)
        {
            DateTime t0 = DateTime.Now;
            Console.WriteLine("[{0}]: Started at {1}", cc.InputPath, t0.ToLocalTime());

            cc.InitializeDocumentSet();

            if (!cc.CheckInput()) return;

            if (cc.IsCreatingDictionary)
            {
                if (!cc.ParseDictionaryInput()) return;
                if (!cc.SaveLexicon()) return;
            }
            else
            {
                if (cc.ProjectId == 0)
                {
                    // �ʏ�̃R�[�p�X�쐬
                    if (!cc.ParseBibFile()) return;
                    if (!cc.InitializeDB()) return;
                    if (!cc.InitializeLexiconBuilder()) return;
                    if (!cc.ParseInput()) return;
                    if (!cc.SaveProject()) return;
                    if (!cc.SaveLexicon()) return; 
                    if (!cc.SaveSentences()) return;
                    if (!cc.SaveSentenceTags()) return;
                    if (!cc.UpdateIndex()) return;
                }
                else
                {
                    // Project�ǉ��̏ꍇ
                    // �E����Lexicon��ǂݍ���ł���
                    // �ESaveSentence�̑����UpdateSentence���Ă�
                    // �EBib, SentenceTag��Index�Ȃǂ̏����͍s��Ȃ�
                    if (!cc.InitializeDB(false)) return;  // Database, Table�̏������͍s��Ȃ�.
                    if (!cc.InitializeLexiconBuilder(true)) return;
                    if (!cc.InitializeTagSet()) return;  // Seg, Link, Group Tag��DB����擾����.
                    if (!cc.ParseInput()) return;
                    if (!cc.AddProject()) return;  // Project Table�̂ݒǉ��X�V
                    if (!cc.SaveLexicon()) return;      // Lexicon�ǉ��́A�V���Ɍ���������̂�.
                    if (!cc.UpdateSentences()) return;
                }
            }
            DateTime t1 = DateTime.Now;
            TimeSpan elapsed = t1 - t0;
            Console.WriteLine("Done.");
            Console.WriteLine("Finished at {0}; Elapsed {1} minutes", t1.ToLocalTime(), elapsed.TotalMinutes);
        }

        static private void DoLUWJobs(CreateCorpus cc)
        {
            // 1. �ʏ��Cabocha�C���|�[�g�����s(Proj 0)
            Console.WriteLine("========================= SUW -> Proj0");
            cc.ReaderType = "Mecab|Cabocha|UniDic2";
            DoJobs(cc);
            // 2. LUW�A�m�e�[�V�������璷�P��Cabocha file���ꎞ�I�ɍ쐬
            Console.WriteLine("=========================");
            Console.WriteLine("Extracting LUW part to temporary cabocha file...");
            var path = Path.GetTempFileName();
            LuwCabochaUtil.Convert(cc.InputPath, path);
            Console.WriteLine($"Written to: {path}");
            // 3. ���P��cabocha file��Proj 1�ɃC���|�[�g
            Console.WriteLine("========================= LUW -> Proj1");
            Console.WriteLine("Importing temporary cabocha file of LUWs...");
            cc.ResetInternals();
            cc.InputPath = path;
            cc.ProjectId = 1;
            DoJobs(cc);
            // 4. �ꎞ�t�@�C�����폜
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cannot delete temp file: {ex.Message}");
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: CreateCorpus [Options] <InputFile> <Output>");
            Console.WriteLine("Options:");
            Console.WriteLine("  [-e=<encoding>] Input Encoding (Auto)");
            Console.WriteLine("  [-t=<type>]     Reader Type; See ReaderDefs.xml (Auto)");
            Console.WriteLine("  [-b=<bibfile>]  Use external .bib file");
            Console.WriteLine("  [-l=<dicfile>]  Use external dictionary");
            Console.WriteLine("  [-d]            Create Dictionary instead of Corpus");
            Console.WriteLine("  [-s]            Set SQLite Synchronous Write mode (false)");
            Console.WriteLine("  [-p]            Make different database for each input when read from folder (false)");
            Console.WriteLine("  [-C]            Do not pause on exit (false)");
            Console.WriteLine("  [-P=<projid>]   Add as a new Project whose id is <projid> (projid > 0)");
            Console.WriteLine("InputFile         cabocha/chasen/mecab/text file");
            Console.WriteLine("Output            .db file for SQLite / .def file for Others / .ddb file for SQLite Dictionary");
        }
    }
}
