using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class TextInterpreter : MonoBehaviour
{
    [SerializeField] private TMP_InputField scriptScreen;
    [SerializeField] private RobotController robotController;

    private RobotSensorModule robotSensor;

    // 型が難しいが、[文字列(関数名)]と[関数の情報(MethodInfo)]をペアで保存する辞書
    private Dictionary<string, MethodInfo> _commandDictionary = new Dictionary<string, MethodInfo>();

    // ↓単語の種類とデータの定義
    public enum TokenType
    {
        Command,    // Move,Turn,CleraLockなどの命令
        Number,     // 0.5や10などの数字
        Control,    // Ifなどの制御文
        Unknown     // 方向などのその他
    }

    public class Token
    {
        public TokenType Type;  // 単語の種類
        public string Value;    // 単語の文字そのもの
        public int LineNumber;  // エラー表示に便利な何行目かのデータ
    }


    private void Start()
    {
        // 安全のためにTextMeshProの内部データを最新にする
        //scriptScreen.ForceMeshUpdate();

        // テストとして、画面の全行を読み込んでログに出力してみる
        //PrintAllLines();

        // ロボット本体のオブジェクトから、センサーモジュールを自動的に取得して繋ぐ
        if (robotController != null && robotSensor == null)
        {
            robotSensor = robotController.GetComponent<RobotSensorModule>();
        }

        SetupCommands();


        
    }

    private void Update()
    {
        
        Execution();
        
    }

    /// <summary>
    /// 指定された行の文字を、一文字ずつつなぎ合わせて一つの文字列にする関数--------------------------------
    /// </summary>
    private string GetLineText(int lineNumber)
    {
        TMP_TextInfo textInfo = scriptScreen.textComponent.textInfo;

        // 指定された行の情報(その行の最初の文字の番号、最後の文字の番号など)を取得
        TMP_LineInfo lineInfo = textInfo.lineInfo[lineNumber];

        StringBuilder lineBuilder = new StringBuilder();

        // その行の「最初のインデックス」から「最後のインデックス」までをループ
        for (int j = lineInfo.firstCharacterIndex; j <= lineInfo.lastCharacterIndex; ++j)
        {
            // 1文字ずつ取り出して繋げる
            lineBuilder.Append(textInfo.characterInfo[j].character);
        }

        // 前後の余計な空白や改行コードを消去して返す
        return lineBuilder.ToString().Trim();
    }

    /// <summary>
    /// 全行をループで回して、GetLineTextの結果をログに出すデバッグ用関数------------------------------------
    /// </summary>
    private void PrintAllLines()
    {
        TMP_TextInfo textInfo = scriptScreen.textComponent.textInfo;

        for (int i = 0; i < textInfo.lineCount; ++i)
        {
            string lineText = GetLineText(i);

            // 空っぽの行(改行だけの行)が無視する
            if (string.IsNullOrWhiteSpace(lineText)) continue;

            Debug.Log($"[{i}行目] : {lineText}");
        }
    }


    /// <summary>
    /// [心臓部]リフレクションでRobotControllerの関数を自動でスキャンして辞書に登録----------------------------
    /// </summary>summary>
    private void SetupCommands()
    {
        // RobotControllerの型情報を取得
        Type robotType = robotController.GetType();

        // RobotScntrollerの中にある[公開されている関数(publicなメソッド)]をすべて引っ張ってくる
        MethodInfo[] methods = robotType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (MethodInfo method in methods)
        {
            // その関数に[RobotCommand]という目印(属性)がついているかをチェック
            if (method.GetCustomAttribute<RobotCommandAttribute>() != null)
            {
                // 関数の名前("Move"など)をキーにして、辞書に登録する
                _commandDictionary[method.Name] = method;
                Debug.Log($"[Reflection] 関数を自動登録しました: {method.Name}");
            }
        }
    }

    /// <summary>
    /// 辞書から関数を探して、文字のデータ(引数)を渡して実行する関数-----------------------------------------
    /// </summary>
    private void ExecuteCommand(string commandName, string[] args)
    {
        // 1.辞書から実行したい関数を探す
        if(_commandDictionary.TryGetValue(commandName, out MethodInfo method))
        {
            // 2.その関数が「どんな型」の引数を「何個」欲しがっているかを取得
            ParameterInfo[] parameterInfos = method.GetParameters();

            // 3.実際にInvokeへ渡すための「完成品を入れる箱」を、要求されている引数の数だけ用意する
            object[] finalArgs = new object[parameterInfos.Length];

            // 4.関数が欲しがっている引数の数だけループして、文字列を型変換していく
            for(int i = 0; i < parameterInfos.Length; ++i)
            {
                // もしテキストから渡された単語の数が、関数が要求する数より少なかったらエラーで止める
                if(i >= args.Length)
                {
                    Debug.LogError($"[Error] {commandName} は引数が {parameterInfos.Length} 個必要ですが、足りません");
                    return;
                }

                Type targetType = parameterInfos[i].ParameterType;// 変換すべき目標の型
                string stringValue = args[i];

                try
                {
                    // 分岐:Enumか、それ以外の基本型か
                    if (targetType.IsEnum)
                    {
                        finalArgs[i] = Enum.Parse(targetType, stringValue, true);
                    }
                    else
                    {
                        // 文字列をfloatやintなどの指定した型に自動変換する
                        finalArgs[i] = Convert.ChangeType(stringValue, targetType);
                    }
                }
                catch (Exception)
                {
                    // 0.5をintにしようとしたりFをfloatにしようとしたとき用の対処
                    Debug.LogError($"[Error] {commandName} の引数　'{stringValue}' を {targetType.Name} 型に変換できませんでした");
                    return;
                }
            }

            // 5.全ての型変換が成功したら、完成した引数パックを渡して実行
            method.Invoke(robotController, finalArgs);
        }
        else
        {
            Debug.LogError($"[Error] {commandName} という目入れは見つかりません");
        }
    }


    /// <summary>
    /// ↓文章を単語に分解してラベルを貼る機能---------------------------------------------------------
    /// </summary>
    private List<Token> LexLine(string line, int lineNumber)
    {
        List<Token> tokens = new List<Token>();

        // 1.スペースやタブで文章を単語ごとに切り分ける
        string[] words = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        // 2.切り分けた単語を順番にチェックしてラベル(TokenType)をはる
        foreach(string word in words)
        {
            // まずは単語の文字と、何行目かのデータをセット
            Token token = new Token { Value = word, LineNumber = lineNumber };

            // その単語がなんの種類化を判定する
            if(_commandDictionary.ContainsKey(word))
            {
                token.Type = TokenType.Command; //  命令文
            }
            else if(word == "If" || word == "End")
            {
                token.Type = TokenType.Control; // 制御文
            }
            else if(float.TryParse(word, out _))
            {
                token.Type = TokenType.Number;  // 数字
            }
            else
            {
                token.Type = TokenType.Unknown;
            }

            tokens.Add(token);
        }

        return tokens;
    }

    /// <summary>
    /// Lexerで分解されたトークンのリストを受け取り、実行エンジンに渡す関数--------------------------------
    /// </summary>
    private void ProcessTokens(List<Token> tokens)
    {
        // トークンが空なら何もしない
        if (tokens.Count == 0) return;

        // 戦闘の単語が命令だったときのみ実行する
        if (tokens[0].Type == TokenType.Command)
        {
            // 1.コマンド名を取得(Move等)
            string commandName = tokens[0].Value;

            // 2.残りの単語をすべて引数としてリストにまとめる
            List<string> argsList = new List<string>();
            for(int i = 1; i < tokens.Count; ++i)
            {
                argsList.Add(tokens[i].Value);  // "F"や"0.5"が入る
            }

            // 3.配列に変換して、実行エンジンに送る
            ExecuteCommand(commandName, argsList.ToArray());
        }
    }


    /// <summary>
    /// 読み取り->トークン化->実行命令の伝達まで行う----------------------------------------------------
    /// </summary>
    private void Execution()
    {

        if (scriptScreen == null) return;
        
        string[] lines = scriptScreen.text.Split(new[] { "\r\n", "\r", "\n"}, StringSplitOptions.None);

        bool isSkipping = false;
        int skipDepth = 0;

        for (int i = 0; i < lines.Length; ++i)
        {
            // 画面から1行持ってくる
            //string lineText = GetLineText(i);
            string lineText = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(lineText)) continue;

            // Lexerで単語を分ける
            List<Token> tokens = LexLine(lineText, i);
            if (tokens == null || tokens.Count == 0) continue;


            string firstWord  = tokens[0].Value;

            // ==========================================
            // 1.スキップモード中の処理(条件不成立でEndを探している状態)-----------------------------------
            // ==========================================
            if (isSkipping)
            {
                if(firstWord == "If")
                {
                    ++skipDepth;
                }
                else if(firstWord == "End")
                {
                    if(skipDepth == 0)
                    {
                        isSkipping = false;
                    }
                    else
                    {
                        --skipDepth;
                    }
                }
                continue;
            }


            // ==========================================
            // 2.通常の実行処理
            // ==========================================
            if(firstWord == "If")
            {
                if(tokens.Count < 4)
                {
                    Debug.LogError($"[{tokens[0].LineNumber}行目] If文の書き方が正しくありません (If 左辺 記号 右辺 の順に書いてください)");
                    continue;
                }

                string left = tokens[1].Value;
                string op = tokens[2].Value;
                string right = tokens[3].Value;

                bool isTrue = EvaluateCondition(left, op, right);

                // もし条件がfalseなら、対応するEndまでスキップモードに入る
                if (!isTrue)
                {
                    isSkipping = true;
                    skipDepth = 0;
                }
            }
            else if(firstWord == "End")
            {
                // 通常実行中にEndに来た場合は、単に条件ブロックの終端を無傷で通り過ぎるだけなのでスルー
                continue;
            }
            else
            {
                // 実行エンジンにパスしてロボットを動かす
                ProcessTokens(tokens);
            }
        }
    }

    /// <summary>
    /// 2つの値を比較して、条件が成立しているかを返す----------------------------------------------------
    /// </summary>
    private bool EvaluateCondition(string left, string op, string right)
    {
        if(left == "EnemyLeft")
        {
            bool isHit = robotSensor.GetSensorData().EnemyLeft;
            return isHit == (right == "True");
        }
        if(left == "EnemyRight")
        {
            bool isHit = robotSensor.GetSensorData().EnemyRight;
            return isHit == (right == "True");
        }
        if(left == "StM")
        {
            bool isOn = robotSensor.GetSensorData().StM;
            return isOn == (right == "True");
        }

        if(op == "==")
        {
            return left == right;
        }
        if(op == "!=")
        {
            return left != right;
        }

        Debug.LogError($"[Error] サポートされていない比較演算子 : {op}");

        return false;
    }
}