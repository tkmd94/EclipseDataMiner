namespace EclipseDataMiner.Models
{
    /// <summary>
    /// 正規表現ヒント・チートシートスニペット項目
    /// </summary>
    public class RegexSnippetItem
    {
        /// <summary>
        /// 正規表現パターン文字列
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// スニペットタイトル（例: 前方一致）
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// スニペットの機能解説
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// マッチする臨床輪郭の具体例
        /// </summary>
        public string Example { get; set; }
    }
}
