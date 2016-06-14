# SharpResize
Реализация упрощенного варианта алгоритма [Least-Squares Image Resizing Using Finite Differences (PDF)](http://bigwww.epfl.ch/publications/munoz0101.pdf), оригинальная версия которого реализована в плагине [Resize](http://bigwww.epfl.ch/algorithms/ijplugins/resize/) для программы обработки изображений [ImageJ](https://ru.wikipedia.org/wiki/ImageJ).


Библиотека реализует упрощенный вариант алгоритма изменения размера изображения (с параметрами, используемыми по-умолчанию в оригинальном плагине).


# Пример работы алгоритма:

![enter image description here](http://bigwww.epfl.ch/algorithms/ijplugins/resize/meta/splash.png)


# Code example:

    var srcImage = new Bitmap(pathToImage);
	var dstImage = srcImage.Resize(900, 600);
