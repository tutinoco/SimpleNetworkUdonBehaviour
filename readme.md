# SimpleNetworkUdonBehaviour
![SimpleNetworkMonster](https://user-images.githubusercontent.com/14051445/215627959-8b82475a-98ff-455e-a744-2724bdc6ce07.png)
SimpleNetworkUdonBehaviourは、VRChatのNetworkingがしんどい方のためのNetworkingラッパーなスーパークラスです。
SendCustomNetworkEventメソッドで、引数を扱えない問題を解消する目的で作成されました。

## 特徴
`SimpleNetworkUdonBehaviour`クラスを継承したサブクラスで`SendEvent`メソッドを実行することで、インスタンス内にいる全ユーザ（自分も含む）の`ReceiveEvent`メソッドにイベント名と値が届きます。
`SendCustomNetworkEvent`のようにメソッドを呼び出すことはできませんが、代わりに`ReceiveEvent`の第一引数にイベント名が届きます。
`MethodInfo`が使用できないため、このような仕様になりましたが、個人的に`SendCustomNetworkEvent`は受信用のメソッドが増えすぎるのでこっちのほうが好みです。

`SendEvent`を連続で実行しても、複数のイベントが積み上げられ、一度の通信で全イベントが届き、最終的に`ReceiveEvent`に連続的に届くので、叩きまくっても安心です。

`SendCustomNetworkEvent`よりも同期速度が早く
また、`SendEvent`の実行者にオブジェクトの所有権がなくても、自動的に`SendEvent`を実行した人に所有権が移ります。

![SyncTest](https://user-images.githubusercontent.com/14051445/216491005-8511b234-ffd3-4b81-938d-b4e069d294b5.mp4)

## 準備
1.  [VRChat Creator Companion](https://github.com/vrchat-community/creator-companion)などで、適当な[UdonSharp](https://github.com/vrchat-community/UdonSharp)プロジェクトを作成または開きます。
2. `Assets`フォルダに`tutinoco`フォルダを作成し、ダウンロードしたSimpleNetworkUdonBehaviourを配置するか`git clone https://github.com/tutinoco/SimpleNetworkUdonBehaviour.git`を実行します。
3. 通信を行いたいオブジェクトを作成または選択してインスペクタから`Add Component`をクリック`Udon Behaviour`を選択します。
4. `Assets`の適当な場所（`Script`の中など）に新しく`U# Script`を作成します。（Projectウインドウで右クリック → Create → U# Script）
5. 新しいU#のクラスを作成したら、3で追加した`Udon Behaviour`コンポーネントの`Program Source`に作成したU# Scriptをドラッグします。
6. 4で作成したU# Scriptを開き、5行目あたりに`using tutinoco;`を追加します。
7. 親クラスが`UdonSharpBehaviour`になっているので`SimpleNetworkUdonBehaviour`に変更します。

## 使い方
1. 引数付きイベントを全ユーザに送信するには`SendEvent`メソッドを実行します。第一引数にイベント名を、第二引数には値となるデータを文字列で入力します。
```例：SendEvent("イベント名","値");```
2. 引数付きイベントを受信するには`ReceiveEvent`メソッドをオーバーライドします。第一引数にはイベント名が、第二引数には値が届きます。
```C#
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using tutinoco;

public class Test : SimpleNetworkUdonBehaviour
{
    public void hoge()
    {
        SendEvent("Talk", "こんにちは！");
    }

    public override void ReceiveEvent(string name, string value)
    {
        if( name == "Talk" ) {
            Debug.Log(value); // こんにちは！
        }
    }
}
```
## 原理
手動同期された文字列の同期変数（コマンドと名称をつけた）に、`SendEvent`の実行によりイベント名とその値をコマンドに追加して手動同期。他のプレイヤーにコマンドが届くので、そのコマンドをOnValueChangedを用いて受信。受信したコマンドに従って`ReceiveEvent`を適切に呼び出します。

## 注意事項
UdonBehaviourSyncModeが強制的にManualに設定されるため、自動同期（Continuous）などとの併用はできません。

また、`OnOwnershipTransferred`メソッドや`OnPreSerialization`メソッドを利用しているため、作成したクラスでも利用したいときは、親クラス（SimpleNetworkUdonBehaviour）にも渡してあげてください。