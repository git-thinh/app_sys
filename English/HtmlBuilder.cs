using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace app_sys
{

    public class HtmlBuilder
    {

        public static string renderFile(string[] lines)
        {
            if (lines.Length == 0) return string.Empty;

            string[] a = lines.Where(x => x.Trim() != "").ToArray();
            Paragraph p;
            List<Paragraph> ls = new List<Paragraph>() { new Paragraph(0, a[0]) };
            string[] atit = a[0].Split('¦');
            if (atit.Length > 1)
                ls.Add(new Paragraph() { id = 0, type = SENTENCE.TAGS, text = atit[1], html = "<p class=tag><em><i>" + string.Join("</i></em>,<em><i>", atit[1].Split(',')) + "</i></em></p>" });

            string si = string.Empty, _code = string.Empty, _ul = string.Empty;
            bool _isCode = false, _isLI = false;
            int _id = 0;
            for (int i = 1; i < a.Length; i++)
            {
                si = a[i];
                if (si == EL._TAG_CODE_CHAR_BEGIN || _isCode)
                {
                    #region [ PRE - CODE ]
                    if (si != EL._TAG_CODE_CHAR_BEGIN) _id = i;

                    _isCode = true;
                    if (si != EL._TAG_CODE_CHAR_BEGIN && si != EL._TAG_CODE_CHAR_END)
                        _code += Environment.NewLine + si;

                    if (i == a.Length - 1 || si == EL._TAG_CODE_CHAR_END)
                    {
                        _isCode = false;
                        string sc = _code.Replace("<", "&#x3C;").Replace(">", "&#x3E;");
                        p = new Paragraph() { id = _id, text = _code, type = SENTENCE.CODE, html = string.Format(@"<{0}>{1}</{0}>", EL.TAG_CODE, sc) };
                        ls.Add(p);
                    }
                    #endregion
                }
                else
                {
                    switch (si[0])
                    {
                        case '■':
                            #region [ HEADING = H3 ]
                            si = si.Substring(1).Trim();
                            p = new Paragraph() { id = i, type = SENTENCE.HEADING, text = si, html = string.Format("<{0}>{1}</{0}>", EL.TAG_HEADING, si.generalHtmlWords()) };
                            ls.Add(p);
                            #endregion
                            break;
                        case '¦':
                            #region [ IMAGE = IMG ]
                            si = si.Substring(1).Trim();
                            p = new Paragraph() { id = i, type = SENTENCE.HEADING, text = si, html = string.Format("<p class=img><{0} src='{1}'/></p>", EL.TAG_IMAGE, si) };
                            ls.Add(p);
                            #endregion
                            break;
                        case '⌐': // begin UL_OL
                            _id = i;
                            _isLI = true;
                            _ul = "<ul>";
                            break;
                        case '•': // LI
                            si = si.Substring(1).Trim();
                            if (_ul != "<ul>") _ul += "</li>";
                            _ul += "<li><p class=li>" + si.generalHtmlWords() + "</p>";
                            break;
                        case '□': // LI LI
                            si = si.Substring(1).Trim();
                            _ul += "<div class=li2>" + si.generalHtmlWords() + "</div>";
                            break;
                        case '▫': // LI LI LI
                            si = si.Substring(1).Trim();
                            _ul += "<div class=li3>" + si.generalHtmlWords() + "</div>";
                            break;
                        case '┘': // end UL_OL
                            _ul += "</ul>";
                            _isLI = false;
                            p = new Paragraph() { id = _id, text = _ul, type = SENTENCE.UL_LI, html = _ul };
                            ls.Add(p);
                            break;
                        #region [ UL_LI ]
                        //case '#':
                        //    si = si.Substring(1).Trim();
                        //    if (_isLI == false)
                        //    {
                        //        _id = i;
                        //        _isLI = true;
                        //        _ul = "<ul><li>" + si.generalHtmlWords() + "</li>";
                        //    }
                        //    else
                        //        _ul += "<li>" + si.generalHtmlWords() + "</li>";

                        //    if (i == a.Length - 1)
                        //    {
                        //        _ul += "</ul>";
                        //        _isLI = false;
                        //        p = new Paragraph() { id = _id, text = _ul, type = SENTENCE.UL_LI, html = _ul };
                        //        ls.Add(p);
                        //    }
                        //    break;
                        #endregion
                        default:
                            #region [ UL_LI ]
                            //if (_isLI)
                            //{
                            //    _ul += "</ul>";
                            //    _isLI = false;
                            //    p = new Paragraph() { id = _id, text = _ul, type = SENTENCE.UL_LI, html = _ul };
                            //    ls.Add(p);
                            //}
                            #endregion

                            if (_isLI)
                            {
                                _ul += "<p class=li_des>" + si.generalHtmlWords() + "</p>";
                            }
                            else
                            {
                                p = new Paragraph(i, si);
                                ls.Add(p);
                            }

                            break;
                    }
                }
            }
            string htm = string.Join(Environment.NewLine, ls.Select(x => x.html).ToArray());
            htm = string.Format("<{0} class=ext_txt>{1}</{0}>", EL.TAG_ARTICLE, htm);

            //string ss = Translator.TranslateText("hello", "en|vi");
            //Console.WriteLine("{0} = {1}", "hello", ss);
            //Console.ReadLine();
            //File.WriteAllText("demo-output.txt", htm);
            return htm;
        }


        public static void fromText(string text_plain)
        {
            string[] a = File.ReadAllLines("demo.txt").Where(x => x.Trim() != "").ToArray();
            Paragraph p;
            List<Paragraph> ls = new List<Paragraph>() { new Paragraph(0, a[0]) };
            string si = string.Empty, _code = string.Empty, _ul = string.Empty;
            bool _isCode = false, _isLI = false;
            int _id = 0;
            for (int i = 1; i < a.Length; i++)
            {
                si = a[i];
                if (si == EL._TAG_CODE_CHAR_BEGIN || _isCode)
                {
                    #region [ PRE - CODE ]
                    if (si != EL._TAG_CODE_CHAR_BEGIN) _id = i;

                    _isCode = true;
                    if (si != EL._TAG_CODE_CHAR_BEGIN && si != EL._TAG_CODE_CHAR_END)
                        _code += Environment.NewLine + si;

                    if (i == a.Length - 1 || si == EL._TAG_CODE_CHAR_END)
                    {
                        _isCode = false;
                        p = new Paragraph() { id = _id, text = _code, type = SENTENCE.CODE, html = string.Format("<{0}>{1}</{0}>", EL.TAG_CODE, _code) };
                        ls.Add(p);
                    }
                    #endregion
                }
                else
                {
                    switch (si[0])
                    {
                        case '*':
                            #region [ HEADING ]
                            si = si.Substring(1).Trim();
                            p = new Paragraph() { id = i, type = SENTENCE.HEADING, text = si, html = string.Format("<{0}>{1}</{0}>", EL.TAG_HEADING, si.generalHtmlWords()) };
                            ls.Add(p);
                            break;
                        #endregion
                        case '#':
                            #region [ UL_LI ]
                            si = si.Substring(1).Trim();
                            if (_isLI == false)
                            {
                                _id = i;
                                _isLI = true;
                                _ul = "<ul><li>" + si.generalHtmlWords() + "</li>";
                            }
                            else
                                _ul += "<li>" + si.generalHtmlWords() + "</li>";

                            if (i == a.Length - 1)
                            {
                                _ul += "</ul>";
                                _isLI = false;
                                p = new Paragraph() { id = _id, text = _ul, type = SENTENCE.UL_LI, html = _ul };
                                ls.Add(p);
                            }
                            break;
                        #endregion
                        default:
                            #region [ UL_LI ]
                            if (_isLI)
                            {
                                _ul += "</ul>";
                                _isLI = false;
                                p = new Paragraph() { id = _id, text = _ul, type = SENTENCE.UL_LI, html = _ul };
                                ls.Add(p);
                            }
                            #endregion

                            p = new Paragraph(i, si);
                            ls.Add(p);
                            break;
                    }
                }
            }
            string htm = string.Join(Environment.NewLine, ls.Select(x => x.html).ToArray());
            htm = string.Format("<{0}>{1}</{0}>", EL.TAG_ARTICLE, htm);

            //string ss = Translator.TranslateText("hello", "en|vi");
            //Console.WriteLine("{0} = {1}", "hello", ss);
            //Console.ReadLine();
            File.WriteAllText("demo-output.txt", htm);
        }
    }

    public class word
    {
        public string w { set; get; }
        public int k { set; get; }
    }
}
