using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class KakikomiManager : MonoBehaviour
{
	[Header("判定したい画像をここへ入れてからゲーム起動")]
	[SerializeField]
	Texture picture;

	[Header("鋭敏度")]
	[SerializeField, Range(0.0f, 2.0f)]
	float sensitivity = 0.1f;

	[Header("---▼ここから↓は触らないで下さい▼---")]
	[SerializeField]
	Material material;

	[SerializeField]
	GameObject originalObj;
	[SerializeField]
	GameObject resultObj;
	[SerializeField]
	Text scoreText;

	Material originalIMat;
	Material resultMat;

	Texture2D originalTexture;
	Color[] originalColors;
	
	Texture2D resultTexture;
	Color[] resultColors;

	Vector2 rangeR = new Vector2(1.0f,0.0f);
	Vector2 rangeG = new Vector2(1.0f, 0.0f);
	Vector2 rangeB = new Vector2(1.0f, 0.0f);

	Texture pastTexture;


	private void Start()
	{
		Exec();
	}

	public void Exec()
	{
		//テクスチャサイズを自由に出来る様にと、ReadWriteをオンに
		var path = AssetDatabase.GetAssetPath(picture);
		var importer = AssetImporter.GetAtPath(path) as TextureImporter;
		importer.npotScale = TextureImporterNPOTScale.None;
		importer.isReadable = enabled;
		importer.SaveAndReimport();

		//テクスチャ差し替え
		material.SetTexture("_BaseMap", picture);

		//テクスチャサイズ計測してオブジェクトサイズ変更
		Vector3 scale = new Vector3((float)picture.width / (float)picture.height, 1.0f, 1.0f);
		originalObj.transform.localScale = scale * 0.2f;
		resultObj.transform.localScale = scale;

		originalIMat = originalObj.GetComponent<Renderer>().material;
		resultMat = resultObj.GetComponent<Renderer>().material;

		originalTexture = (Texture2D)originalIMat.mainTexture;
		resultTexture = (Texture2D)resultMat.mainTexture;

		originalColors = originalTexture.GetPixels();
		resultColors = resultTexture.GetPixels();

		//テクスチャ情報を読み取って配列に変換

		resultColors = new Color[originalColors.Length];
		originalColors.CopyTo(resultColors, 0);



		//画面全体の色の最小、最大値を読み取る
		for (int x = 0; x < originalTexture.width; x++)
		{
			for (int y = 0; y < originalTexture.height; y++)
			{
				int a = x + y * originalTexture.width;

				if (originalColors[a].r < rangeR.x)
					rangeR.x = originalColors[a].r;
				if (originalColors[a].g < rangeG.x)
					rangeG.x = originalColors[a].g;
				if (originalColors[a].b < rangeB.x)
					rangeB.x = originalColors[a].b;

				if (originalColors[a].r > rangeR.y)
					rangeR.y = originalColors[a].r;
				if (originalColors[a].g > rangeG.y)
					rangeG.y = originalColors[a].g;
				if (originalColors[a].b > rangeB.y)
					rangeB.y = originalColors[a].b;
			}
		}

		float score = 0.0f;

		//１ピクセル単位で計測
		for (int x = 0; x < originalTexture.width; x++)
		{
			for (int y = 0; y < originalTexture.height; y++)
			{
				//端っこのピクセルは計算除外
				if (y != 0 && y != originalTexture.height - 1 && x != 0 && x != originalTexture.width - 1)
				{
					//サンプリングしたいピクセルの周り８ピクセルのID取得
					int[] ids = new int[9]
					{
						x - 1 + (y-1) * originalTexture.width,
						x + (y-1) * originalTexture.width,
						x + 1 + (y-1) * originalTexture.width,
						x - 1 + y * originalTexture.width,
						x + y * originalTexture.width,
						x + 1 + y * originalTexture.width,
						x - 1 + (y+1) * originalTexture.width,
						x + (y+1) * originalTexture.width,
						x + 1 + (y+1) * originalTexture.width
					};

					//描き込み度合い計算
					float kakikomi = Calcu1Pixel(originalColors, ids);

					//結果の配列に保存
					resultColors[x + y * originalTexture.width] = Color.white * (1.0f - kakikomi);

					//スコア計算
					score += kakikomi / (originalTexture.width * originalTexture.height);
				}
			}
		}

		//この係数は適当
		score *= Mathf.Round(100000.0f);

		Debug.Log(score);

		this.scoreText.text = (score).ToString("f0");

		//結果をテクスチャとして視覚化
		resultTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);
		resultTexture.filterMode = FilterMode.Point;
		resultTexture.SetPixels(resultColors);
		resultTexture.Apply();
		resultMat.mainTexture = resultTexture;
	}


	float Calcu1Pixel(Color[] colors, int[] id)
	{
		float sR = 0.0f;
		float sG = 0.0f;
		float sB = 0.0f;

		Color[] c = new Color[9];

		//9ピクセルを最大、最小値と照らし合わせて、0～1の値に補正
		//画面全体が暗かった（明るかった）としても判定出来る様に
		for(int i= 0;i<c.Length;i++)
		{
			c[i].r = Remap(colors[id[i]].r, rangeR.x, rangeR.y);
			c[i].g = Remap(colors[id[i]].g, rangeG.x, rangeG.y);
			c[i].b = Remap(colors[id[i]].b, rangeB.x, rangeB.y);
		}


		for (int i = 0; i < id.Length; i++)
		{
			//中心ピクセルは除外
			if (i == 5)
				continue;

			//中心ピクセルとの色比較
			float tR = Mathf.Abs(c[i].r - c[5].r);
			if (tR > sR)
				sR = tR;
			float tG = Mathf.Abs(c[i].g - c[5].g);
			if (tG > sG)
				sG = tG;
			float tB = Mathf.Abs(c[i].b - c[5].b);
			if (tB > sB)
				sB = tB;
		}

		//最も変化量が大きかった値を採用
		float s = Mathf.Max(Mathf.Max(sR, sG), sB);

		return Mathf.Pow(s, 1/sensitivity);
	}

	float Remap(float value, float min, float max)
	{
		if (max - min < float.Epsilon)
			return 0.0f;

		return (value - min) / (max - min);
	}




}
