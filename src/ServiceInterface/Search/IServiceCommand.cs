using ChaKi.Entity.Kwic;
using System;

namespace ChaKi.Service.Search
{
    public interface IServiceCommand
    {
        /// <summary>
        /// コマンドの実行を開始する
        /// </summary>
        void Begin();

        EventHandler Completed { get; set; }

        EventHandler Aborted { get; set; }
    }
}
