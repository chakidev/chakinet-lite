using ChaKi.Entity.Corpora;
using ChaKi.Entity.Kwic;
using ChaKi.Entity.Search;

namespace ChaKi.Common
{
    public delegate void CurrentChangedDelegate(Corpus cps, int senid);
    public delegate void RequestContextDelegate(KwicList list, int row);
    public delegate void RequestDepEditDelegate(KwicItem ki);
    public delegate void UpdateGuidePanelDelegate(Corpus corpus, Lexeme lex);
    public delegate void NavigateHistoryDelegate(SearchHistory hist);
    public delegate void ChangeSentenceDelegate(Corpus crps, int currentSenid, int direction, bool moveInKwicList);
    public delegate void UpdateAttributePanelDelegate(Corpus crps, object source);
}
