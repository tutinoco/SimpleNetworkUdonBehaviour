# SimpleNetworkUdonBehaviour
![SimpleNetworkMonster](https://user-images.githubusercontent.com/14051445/215627959-8b82475a-98ff-455e-a744-2724bdc6ce07.png)
SimpleNetworkUdonBehaviourは、VRChatのNetworkingがしんどい方のためのNetworkingラッパーなスーパークラスです。
SendCustomNetworkEventメソッドで、引数を扱えない問題を解消する目的で作成されました。

## 特徴
* `SendCustomNetworkEvent`では不可能な、引数の送信を実現します。
* `SendCustomNetworkEvent`よりも低レイテンシーな高速同期ができます。
* 連続でイベントを叩いても自動的に1回の通信にまとめるから安定して動作します。
* 最後の通信情報がサーバに保存され、その情報をJoinした人に同期する設定を切替できます。
* イベント発信者を制限する機能を搭載し、コードをシンプルに書けます。
* わずらわしい権限問題も気にしなくてOK！ Forceモードを使えば、所有権を取得してのイベント送信が可能。

`SimpleNetworkUdonBehaviour`クラスを継承したサブクラスで`SendEvent`メソッドを実行することで、インスタンス内にいる全ユーザ（自分も含む）にイベント名と値を送信することができます。
イベントの受信は、サブクラスで`ReceiveEvent`メソッドをオーバーライドすることで可能となり、第一引数にイベント名が、第二引数に値が届きます。

`SendCustomNetworkEvent`のようにメソッドを呼び出すことはできません。
Udonでは、`MethodInfo`が使用できないため、このような仕様になりましたが、`SendCustomNetworkEvent`は受信用メソッドが増えすぎてしまうため、個人的にこっちのほうが好みです。

オブジェクトの`UdonBehaviourSyncMode`を`Manual`に設定すると`SendCustomNetworkEvent`よりも高速にイベントが届く、低レイテンシー同期が有効になります。
https://user-images.githubusercontent.com/14051445/216792466-d6fc23c1-0b0e-436a-a7b9-fdac29b0fac2.mp4

また、`SendEvent`をfor分で繰り返すなど連続で実行しても、複数のイベントが自動でまとまって一度の通信で全イベントを届けるため、安定動作します。

インスタンスマスターや、オブジェクトオーナーが代表してイベント送信を行う方法でプログラミングする際、`SendCustomNetworkEvent`では、`if( Networking.isMaster ) { ... }`や`Networking.IsOwner(gameObject) ) { ... }`のようなコードが繰り返されますが、`SimpleNetworkInit`メソッドの第一引数の`publisher`に`SimpleNetwork.Owner`などの指定を行うことで、イベントの発行者を限定することができます。
また、`if( IsPublisher() ) { ... }`を利用することで、イベント発行前の処理の実行も制御できるほか、`SendEvent`の第三引数のForceモードを使うことで、一時的に誰でもイベントを送信させることもできます。
この時、オブジェクトの所有権は、自動的に`SendEvent`を実行した人に移ります。

## テストワールド
下記のリンクからSimpleNetworkUdonBehaviourの挙動を実際に確認することができます。
https://vrchat.com/home/world/wrld_11b7ced1-63a6-49cc-ac2f-8b2537bf435d

## 準備
1.  [VRChat Creator Companion](https://github.com/vrchat-community/creator-companion)などで、適当な[UdonSharp](https://github.com/vrchat-community/UdonSharp)プロジェクトを作成または開きます。
1. `Assets`フォルダに`tutinoco`フォルダを作成し、ダウンロードした`SimpleNetworkUdonBehaviour`を配置するか`git clone https://github.com/tutinoco/SimpleNetworkUdonBehaviour.git`を実行します。
1. Projectウインドウで右クリック → Create → U# Scriptを選択すると、新しいスクリプトの保存先とファイル名を聞かれるので`Assets/Scripts/TestTest.cs`などで作成します。
1. 通信を行いたいオブジェクトを作成または選択してインスペクタから`Add Component`をクリック、先ほど作成したスクリプトファイル名`TestTest`を選択します。（高速同期を有効にするには、ここで`UdonBehaviourSyncMode`を`Manual`に設定する）
1. 3で作成したU# Scriptを開き、6行目あたりに`using tutinoco;`を追加します。
1. 親クラスが`UdonSharpBehaviour`になっているので`SimpleNetworkUdonBehaviour`に変更します。

## 使い方
1. サブクラスの`Start`メソッドで、`SimpleNetworkInit`を呼び出し、利用前に`SimpleNetworkUdonBehaviour`を初期化します。
1. 引数付きイベントを全ユーザ（自分を含む）に送信するには`SendEvent`メソッドを実行します。第一引数にイベント名を、第二引数には値となるデータを設定します。
```例：SendEvent("イベント名", "値");```
1. 引数付きイベントを受信するには`ReceiveEvent`メソッドをオーバーライドします。第一引数にはイベント名が、第二引数には値が全ユーザ（自分を含む）に届きます。
```C#
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tutinoco;

public class Test : SimpleNetworkUdonBehaviour
{
    void Start()
    {
        SimpleNetworkInit();
        SendEvent("Talk", "こんにちは！");
    }

    public override void ReceiveEvent(string name, string value)
    {
        if( name == "Talk" ) {
            Debug.Log(value); // こんにちは！が全ユーザに届く
        }
    }
}
```
`SendEvent`の第三引数を`true`にすると、パブリッシャー（イベント発行が許された者）でなくともイベントを送信することができます。もしパブリッシャーでなければ、自動的にオブジェクトの所有権を獲得してイベントを送信するため、オブジェクトの所有権を気にすることなくイベントを送信することができます。
```例：SendEvent("イベント名", "値", SimpleNetwork.All);```

### 対応している型
送受信に対応している方は、現在`bool` `int` `float` `Vector3`です。
`GetBool` `GetInt` `GetFloat` `GetVector3` のメソッドを利用して受信したデータを元の型に変換します。

以下は`Vector3`の送受信の例です。
```C#
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tutinoco;

public class Test : SimpleNetworkUdonBehaviour
{
    void Start()
    {
        SimpleNetworkInit();

        Vector3 v = new Vector3(1.0f, 2.0f, 3.0f);
        SendEvent("SetPosition", v);
    }

    public override void ReceiveEvent(string name, string value)
    {
        if( name == "SetPosition" ) {
            gameObject.transform.position = GetVector3(value);
        }
    }
}
```

## 原理
手動同期された文字列の同期変数（コマンドと名称をつけた）に、`SendEvent`の実行によりイベント名とその値をコマンドに追加して手動同期。他のプレイヤーにコマンドが届くので、そのコマンドをOnValueChangedを用いて受信。受信したコマンドに従って`ReceiveEvent`を適切に呼び出します。

## 注意事項
`OnOwnershipTransferred`と`OnPreSerialization`メソッドを利用しているため、サブクラスでも利用したいときは、親クラス（SimpleNetworkUdonBehaviour）にも渡してあげる必要があります。
複数コマンドの一括受信に対応するため`･`（半角中黒）を利用しているため、文字列の送信に`･`を使うことはできません。