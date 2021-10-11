set PATH=..\..\..\..\bin\release;%PATH%

CreateCorpusSLA -e=UTF-8 -C -d LexemeEditServiceTestData1.dic LexemeEditServiceTestData1.ddb
CreateCorpusSLA -e=UTF-8 -C LexemeEditServiceTestData2.mecab LexemeEditServiceTestData2.db
CreateCorpusSLA -e=UTF-8 -C LexemeEditServiceTestData4.mecab LexemeEditServiceTestData4.db
CreateCorpusSLA -e=UTF-8 -C -d LexemeEditServiceTestData5.dic LexemeEditServiceTestData5.ddb
CreateCorpusSLA -e=UTF-8 -C LexemeEditServiceTestData6.mecab LexemeEditServiceTestData6.db
CreateCorpusSLA -e=UTF-8 -C MergeMultiSentenceTestData.cabocha MergeMultiSentenceTestData.db
CreateCorpusSLA -e=UTF-8 -C MergeSentenceNotThrowCountMismatchExceptionTestData.cabocha MergeSentenceNotThrowCountMismatchExceptionTestData.db
CreateCorpusSLA -e=UTF-8 -C SentenceEditServiceTestData2.cabocha SentenceEditServiceTestData2.db
CreateCorpusSLA -e=UTF-8 -C SplitSentenceTestData.cabocha SplitSentenceTestData.db
