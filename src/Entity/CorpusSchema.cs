using System;
using System.Collections.Generic;
using System.Text;

namespace ChaKi.Entity
{
    public class CorpusSchema
    {
        public static readonly string KeyName = "corpus_schema";

        //SchemaVersion 13 @ 2017.05.30
        //SchemaVersion 12 @ 2016.08.15
        //SchemaVersion 11 @ 2016.01.07
        //SchemaVersion 10 @ 2014.07.08
        //SchemaVersion 9 @ 2014.04.29
        //SchemaVersion 8 @ 2013.01.14
        //SchemaVersion 7 @ 2011.07.05
        //SchemaVersion 6 @ 2010.12.25
        //SchemaVersion 5 @ 2010.11.02
        //SchemaVersion 4 @ 2010.01.19
        //SchemaVersion 3 @ 2009.12.14
        //SchemaVersion 2 @ 2009.11.27
        //SchemaVersion 1 @ 2009.08.26
        public static readonly int CurrentVersion = 13;

        //   // DBのスキーマバージョン(corpus_attributeテーブルから得られる)
        public int Version { get; set; }
    }
}
