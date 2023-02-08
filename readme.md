# SimpleNetworkUdonBehaviour
![SimpleNetworkMonster](https://user-images.githubusercontent.com/14051445/217167649-049ccedd-4170-48e7-be75-e1ff21214164.png)
SimpleNetworkUdonBehaviourは、VRChatのNetworkingがしんどい方のためのNetworkingラッパーなスーパークラスです。
SendCustomNetworkEventメソッドで、引数を扱えない問題を解消する目的で作成されました。

## テストワールド
![TestWorldScreenShot](https://user-images.githubusercontent.com/14051445/217113394-8ab44d65-20f1-4e34-9d32-caf2a461e3dc.png)
下記のリンクからSimpleNetworkUdonBehaviourの挙動を実際に確認することができます。
https://vrchat.com/home/world/wrld_11b7ced1-63a6-49cc-ac2f-8b2537bf435d

一人では確認できないので、Inviteを送って友達と一緒に見てみてね！

## 特徴
* `SendCustomNetworkEvent`では不可能な、引数の送信を実現します。
* `SendCustomNetworkEvent`よりも低レイテンシーな高速同期ができます。
* 連続でイベントを叩いても自動的に1回の通信にまとめるから安定して動作します。
* 最後に実行したイベントやイベントログをサーバに保存し、Joinした人に同期できます。
* イベント発行者を制限する機能を搭載し、予期せぬイベント発行を防ぎます。
* わずらわしい権限問題もForceモードを使えば、自動的に所有権を取得しての送信が可能です。

## 説明
VRChatでUdonを使ってネットワークプログラミングするためには、プログラムの流れの中で複数台のコンピュータを意識し
どのコンピュータがどういう状態にあるかを考えながらプログラミングすることが避けられませんでした。

Udonを使って、ネットワーク上の他のコンピュータの同じオブジェクトのメソッドを呼び出すには`SendCustomNetworkEvent`を使うのが一般的です。
しかし`SendCustomNetworkEvent`は、メソッドを呼び出せるものの、そのメソッドに値を送れないという最大の問題を抱えていました。

`SimpleNetworkUdonBehaviour`は、VRChatに用意された、`UdonSynced`の仕組みを利用してカスタムイベントをコマンド化し
`OnValueChanged`を利用して、コマンドが同期されたタイミングで受信用メソッドを呼び出すことで、引数付きのイベント発行を実現しました。

また、`UdonSynced`は高速同期されるため、`SendCustomNetworkEvent`よりも早い応答速度の通信を実現しています。
この低レイテンシー同期は、オブジェクトの`UdonBehaviourSyncMode`を`Manual`に設定することで有効になります。

![speedtest](https://user-images.githubusercontent.com/14051445/216792466-d6fc23c1-0b0e-436a-a7b9-fdac29b0fac2.mp4)

`SimpleNetworkUdonBehaviour`の`SendEvent`を利用したネットワークイベントの仕組みは、for文で連続実行してもネットワークに負荷をかけません。
`SimpleNetworkUdonBehaviour`は複数のイベントを自動でひとつにまとめ、一度の通信で全イベントを届けるため、安定して動作します。

また、`SendCustomNetworkEvent`を使うことで引きおこる変化は、基本的にLate-Joiner（後からJoinした人）に同期してくれませんが
`SimpleNetworkUdonBehaviour`には、JoinSync（ジョインシンク）という機能が備わっており、Late-Joinerに指定のイベントを実行したり、
連続した指定のイベントをログという形でサーバに保存し、すべてのログ化されたイベントをLate-Joinerに連続的に実行して復元する機能も備わっています。

`SimpleNetworkUdonBehaviour`には、安全なコードを単純に記述する仕組みも備わっています。
ネットワークプログラミングではインスタンスマスターや、オブジェクトオーナーが代表してイベントの送信を担うことがあり
`if( Networking.IsOwner(gameObject) ) { ... }`のようなコードが乱立しがちです。

`SimpleNetworkUdonBehaviour`には、Publisher（パブリッシャー）という概念が備わっており、初期化時にこのパブリッシャーを設定することで、イベント発行者を限定することができます。
この仕組みを使うことで、誤って予期せぬ人から同じイベントが送信されるといったこと防ぐことができるため、どのネットワークイベントを誰が送信しているのかを意識してプログラミングする必要がありません。

また、`IsPublisher`メソッドを使って、パブリッシャーのみ実行する処理を書いたり
`SendEvent`のForceモードを使うことで、パブリッシャーでなくても強制的にイベントを送信することもできます。

## 使い方
`SimpleNetworkUdonBehaviour`クラスを継承し、`SendEvent`メソッドを実行することで、インスタンス内にいる全ユーザ（自分も含む）の同一オブジェクトにイベント名と値を届けることができます。
イベントの受信は、サブクラスで`ReceiveEvent`メソッドをオーバーライドすることで可能となり、第一引数にイベント名が、第二引数に値が届きます。

1. `SimpleNetworkUdonBehaviour`を初期化するため、`Start`メソッドで`SimpleNetworkInit`呼び出します。
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
上記のコードでは、わかりやすさを優先して`SendEvent`メソッドを`Start`に記述しています。

自らのイベントを自らで呼ぶ使い方もできますが、嬉しいのは、値の送信が可能になったことで、命令されたら動くアクションだけをまとめたクラスを作成できるようになることです。

以下は、命令待ちをするモンスタークラスです。
```C#
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tutinoco;

public class Monster : SimpleNetworkUdonBehaviour
{
    [SerializeField] private Rigidbody rigidbody;
    [SerializeField] private Text fukidashi;

    void Start()
    {
        SimpleNetworkInit();
    }

    public override void ReceiveEvent(string name, string value)
    {
        // 上にジャンプする
        if( name == "jump" ) {
            float power = GetFloat(value);
            rigidbody.AddForce(transform.up*power, ForceMode.Impulse);
        }

        // 前にダッシュする
        if( name == "dash" ) {
            float power = GetFloat(value);
            rigidbody.AddForce(transform.forward*power, ForceMode.Impulse);
        }

        // ふきだしに文字を表示
        if( name == "talk" ) {
            fukidashi.text = value;
        }
    }
}
```
このモンスタークラスを使って作られたモンスターをジャンプさせるには、適当な場所に以下のコードを記述します。
```C#
monster.SendEvent("jump", 5.0f);
```
もちろん値を送信できるため、ここでは`float`型のジャンプ力の指定も行ってイベントを送信しています。

これらモンスターオブジェクトを配列で管理すれば、複数のモンスターを同時に制御することも可能です。
```C#
// 複数のモンスターをいろんな強さでジャンプさせる
//（もちろん、どのパソコンから見ても同じように見えるよ！）
foreach( monster in monsters ) {
    float power = Random.Range(3.0f, 6.0f);
    monster.SendEvent("jump", power);
}
```

### 対応しているデータ型
`SimpleNetworkUdonBehaviour`は、基本的に文字列データの送受信を行いますが、`Vector3`などの型にも対応しています。
既に前述のコードでは、`ReceiveEvent`メソッド内で`GetFloat`を用いて`float`型のデータを受け取っています。

対応している型は、現在`bool` `int` `float` `Vector3`です。

`ReceiveEvent`で受信したデータは`string`に変換されているため
`GetBool` `GetInt` `GetFloat` `GetVector3` 等のメソッドを利用し、元の型に戻して受け取る必要があります。

現在非対応の型に対応してくださった方がいらっしゃいましたら、是非共有してくださると嬉しいです:)

### パブリッシャー
`SimpleNetworkInit`メソッドを利用して初期化するとき、第一引数にパブリッシャーを設定して初期化することができます。

パブリッシャーとは、イベント発行が許されたユーザを限定する安全装置的な機能です。
パブリッシャーを設定しないと`Publisher.All`が自動的に選択され、誰でもイベントを送信できる状態となります。

* `Publisher.All` 全ての人がパブリッシャーとなり、誰でも`SendEvent`の利用が可能になります。
* `Publisher.Owner` オブジェクト所有者のみ`SendEvent`の利用が可能になります。
* `Publisher.Master` インスタンスマスターのみ`SendEvent`の利用が可能になります。

この仕組みを使うことで、誤って予期せぬ人から同じイベントが送信されるといったこと防ぐことができるため
どのネットワークイベントを誰が送信しているのかを意識してプログラミングする必要がありません。
```C#
public class Sample : SimpleNetworkUdonBehaviour
{
    void Start()
    {
        // オブジェクトの所有者のみSendEventの実行を許す
        SimpleNetworkInit( Publisher.Owner );
    }

    void Update()
    {
        // パブリッシャーがOwnerに設定されているため
        // オブジェクト所有者しかSendEventを実行できない
        // そのためOwnerの座標だけが全員に送信される
        SendEvent("SetPosition", gameObject.transform.position);
    }

    public override void ReceiveEvent(string name, string value)
    {
        if( name == "SetPosition" ) {
            // オーナー以外は受け取った座標を反映
            if( !IsPublisher() ) gameObject.transform.position(GetVector3(value));
        }
    }
}
```
パブリッシャーの設定は安全装置的な機能ですが、`IsPublisher`メソッドを使ってパブリッシャーに明示的に処理を行わせたりすることもできます。
上記の`ReceiveEvent`内のコードでは、`IsPublisher`メソッドを利用し、イベントを送信してきたパブリッシャー以外のみ座標を同期するような制御をしています。

### Forceモード
`SendEvent`には、最後の引数にForceモードが存在し`true`に設定することで、パブリッシャー（イベント発行が許された者）でなくとも、一時的にイベントを送信することができます。
このとき`SimpleNetworkUdonBehaviour`は、自動的にオブジェクトの所有権を獲得し、その後でイベントを送信します。
```C#
// Forceモードでイベントを強制的に発行
SendEvent("イベント名", "値", true);
```

パブリッシャーを`Publisher.Owner`にしていた場合、Forceモードを利用してイベントを強制的に発行すると、オブジェクト所有者がその人に移るため、パブリッシャーが切り替わって、その後のイベントはその人が担うことになります。

### JoinSync
`SimpleNetworkUdonBehaviour`には、イベントをサーバに記録し、Late-Joiner（後からJoinした人）に実行させる機能が備わっています。
この機能をうまく利用することで、複雑なプログラミング無しにLate-Joinerに状態を正しく同期させることができます。

`JoinSync`は`SendEvent`メソッドの第三引数に設定します。
```C#
// 1000円で売るイベントをサーバにログとして積み上げて送信
SendEvnet("sale", 1000, JoinSync.Logging);
```
設定できる`JoinSync`の値は以下の通りです。

* `JoinSync.None` サーバにイベントを保存せず、Late-Joinerにこのイベントは送信しません。
* `JoinSync.Latest` 同名のイベントの中で、最後に実行されたイベントと値をサーバに保存し、Late-Joinerに送信します。
* `JoinSync.Logging` 同名のイベントの中で、今まで実行されたイベントを全てサーバに保存し、Late-Joinerに連続的に送信します。

サーバに記録されたイベントやイベントログを削除するには`ClearJoinSyncEvent`メソッドを利用します。
```C#
// 売ったイベントのログをサーバから削除する
ClearJoinSyncEvent("sale");
```

オブジェクトに蓄積された、あらゆる全てのイベントを削除するにはイベント名を指定せずに実行します。
```C#
// オブジェクトが持つ全てのJoinSyncの記録をサーバから削除
ClearJoinSyncEvent();
```

### ExecEvent
Udonでは、イベントのローカル実行に`SendCustomEvent`が用意されていますが、似たように自身のPCのみで`ReceiveEvent`を呼ぶ方法に`ExecEvent`メソッドが用意されています。
```C#
// イベントを送信せず自分のみイベントを実行する
ExecEvent("イベント名", "値");
```
`ExecEvent`はローカル動作するため、JoinSyncを設定したり、Forceモードで実行したりすることはできません。

## 導入
1.  [VRChat Creator Companion](https://github.com/vrchat-community/creator-companion)などで、適当な[UdonSharp](https://github.com/vrchat-community/UdonSharp)プロジェクトを作成または開きます。
1. `Assets`フォルダに`tutinoco`フォルダを作成し、ダウンロードした`SimpleNetworkUdonBehaviour`を配置するか`git clone https://github.com/tutinoco/SimpleNetworkUdonBehaviour.git`を実行します。
1. Projectウインドウで右クリック → Create → U# Scriptを選択すると、新しいスクリプトの保存先とファイル名を聞かれるので`Assets/Scripts/TestTest.cs`などで作成します。
1. 通信を行いたいオブジェクトを作成または選択してインスペクタから`Add Component`をクリック、先ほど作成したスクリプトファイル名`TestTest`を選択します。（高速同期を有効にするには、ここで`UdonBehaviourSyncMode`を`Manual`に設定する）
1. 3で作成したU# Scriptを開き、6行目あたりに`using tutinoco;`を追加します。
1. 親クラスが`UdonSharpBehaviour`になっているので`SimpleNetworkUdonBehaviour`に変更します。

## 注意事項
* `SendCustomNetworkEvent`のようにメソッドを呼び出すことはできません。
Udonでは、`MethodInfo`が使用できないため、このような仕様になりましたが、`SendCustomNetworkEvent`は受信用メソッドが増えすぎてしまうため、個人的にこっちのほうが好みです。
* `OnOwnershipTransferred`と`OnPreSerialization`メソッドを利用しているため、サブクラスでも利用したいときは、親クラス`SimpleNetworkUdonBehaviour`にも渡してあげる必要があります。
* 複数コマンドの一括受信に対応するため`･`（半角中黒）を利用しています。そのため、文字列の送信に`･`を使うことはできません。
* イベント名に`:`と`__init__`は利用できません。