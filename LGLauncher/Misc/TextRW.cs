using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

/*
 *   - テキストファイルの読み書きにファイル共有設定をつける。
 *     System.IO.File.ReadAllLines();は別のプロセスが使用中のファイルを読み込めなかった。
 *   - アセンブリリソースの読込み 
 */
namespace LGLauncher
{
  /// <summary>
  /// 文字エンコード
  /// </summary>
  /// <remarks>
  ///  *.ts.program.txt        Shift-JIS
  /// 
  ///  avs, d2v, lwi, bat      Shift-JIS
  ///  vpy                     UTF8_nobom
  ///  srt                     UTF8_bom
  /// </remarks>
  class TextEnc
  {
    public static readonly
      Encoding Ascii = Encoding.ASCII,
               Shift_JIS = Encoding.GetEncoding("Shift_JIS"),
               UTF8_nobom = new UTF8Encoding(false),
               UTF8_bom = Encoding.UTF8
               ;
  }



  #region TextR

  /// <summary>
  /// 共有設定を付けてテキストを読み込む
  /// </summary>
  class TextR
  {
    /// <summary>
    /// 共有設定を付けてテキストを読込む
    /// </summary>
    public static List<string> ReadAllLines(string path, Encoding enc = null)
    {
      enc = enc ?? TextEnc.Shift_JIS;
      try
      {
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
          if (100 * 1024 * 1024 < fs.Length)
            throw new Exception("read large file");
          using (var reader = new StreamReader(fs, enc))
          {
            var text = new List<string>();
            while (!reader.EndOfStream)
              text.Add(reader.ReadLine());
            return text;
          }
        }
      }
      catch
      {
        return null;
      }
    }

    /// <summary>
    /// 共有設定を付けてバイナリファイルを読込む
    /// </summary>
    public static byte[] ReadBytes(string path)
    {
      try
      {
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
          if (100 * 1024 * 1024 < fs.Length)
            throw new Exception("read large file");
          using (var reader = new BinaryReader(fs))
          {
            var data = new List<byte>();
            while (true)
            {
              byte[] d = reader.ReadBytes(32 * 1024);
              if (d.Count() == 0)
                break;
              else
                data.AddRange(d);
            }
            return data.ToArray();
          }
        }
      }
      catch
      {
        return null;
      }
    }


    /// <summary>
    /// アセンブリ内のリソース読込み
    /// </summary>
    /// <remarks>
    /// リソースが存在しないとnew StreamReader(null,enc)で例外
    /// bat, avs        Shift-JIS
    /// vpy             UTF8_nobom
    /// </remarks>
    public static List<string> ReadFromResource(string name, Encoding enc = null)
    {
      enc = enc ?? TextEnc.Shift_JIS;
      //マニフェストリソースからファイルオープン
      var assembly = Assembly.GetExecutingAssembly();
      var reader = new StreamReader(assembly.GetManifestResourceStream(name), enc);
      //read
      var text = new List<string>();
      while (!reader.EndOfStream)
        text.Add(reader.ReadLine());
      reader.Close();
      return text;
    }

  }
  #endregion



  #region LTextR
  /// <summary>
  /// lwi読込み用
  /// </summary>
  class LTextR
  {
    public bool IsOpen { get { return reader != null; } }
    private FileStream fstream;
    private StreamReader reader;

    /// <summary>
    /// Constructor
    /// </summary>
    public LTextR(string path)
    {
      try
      {
        fstream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        reader = new StreamReader(fstream, TextEnc.Shift_JIS);
      }
      catch { /* do nothing */ }
    }
    ~LTextR()
    {
      Close();
    }

    /// <summary>
    /// Close
    /// </summary>
    public void Close()
    {
      if (reader != null)
        reader.Close();
    }

    /// <summary>
    /// Ｎ行読込む
    /// </summary>
    /// <param name="NLines">読込行数</param>
    /// <returns>
    /// ０～Ｎ行
    /// EOFに到達するとNLinesに満たない。
    /// </returns>
    public List<string> ReadLines(int NLines)
    {
      var text = new List<string>();
      for (int i = 0; i < NLines; i++)
      {
        string line = reader.ReadLine();
        if (line != null)
          text.Add(line);
        else
          break;
      }
      return text;
    }
  }

  #endregion



  #region LTextW
  /// <summary>
  /// lwi書込み用
  /// </summary>
  class LTextW
  {
    public bool IsOpen { get { return writer != null; } }
    private FileStream fstream;
    private StreamWriter writer;

    /// <summary>
    /// Constructor
    /// </summary>
    public LTextW(string path)
    {
      try
      {
        fstream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        writer = new StreamWriter(fstream, TextEnc.Shift_JIS);
      }
      catch { /* do nothing */ }
    }
    ~LTextW()
    {
      Close();
    }

    /// <summary>
    /// 閉じる
    /// </summary>
    public void Close()
    {
      if (writer != null)
        writer.Close();
    }

    /// <summary>
    /// 改行コードを"\n"に変更
    /// </summary>
    public void SetNewline_n()
    {
      writer.NewLine = "\n";
    }

    /// <summary>
    /// テキスト書込み
    /// </summary>
    public void WriteLine(string line)
    {
      writer.WriteLine(line);
    }

    /// <summary>
    /// テキスト書込み
    /// </summary>
    public void WriteLine(IEnumerable<string> text)
    {
      foreach (var line in text)
        writer.WriteLine(line);
    }

    /// <summary>
    /// バイナリ書込み
    /// </summary>
    public void WriteByte(IEnumerable<byte> data)
    {
      //FileStreamに直接書き込むので、先にStreamWriterをFlush()しなくてはいけない。
      //WriteByte()は一度しか利用しないのでこれで対応する。
      writer.Flush();
      foreach (var d in data)
        fstream.WriteByte(d);
      fstream.Flush();
    }
  }

  #endregion 

}