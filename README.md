# ScincNet

![alt text](https://github.com/pbbwfc/ScincNet/blob/main/docs/images/Screen.png "Screen")

## **S**hane's **C**hess **I**nformation database - **N**o **C**omplications using .**Net**

 The backend is based on the excellent [Scidvspc](http://scidvspc.sourceforge.net/ "Scidvspc").

 This repository includes a .NET wrapper to access this back end. This allow scripting in F# against SCID databases.

 The repository also includes a trimmed down front end, built using WinForms written in F#.

For further details, see the documentation:
[https://pbbwfc.github.io/ScincNet/](https://pbbwfc.github.io/ScincNet/)

## Using

To get started please install using the **setup.exe** in [Releases](https://github.com/pbbwfc/ScincNet/releases).

## Developing

All development is done using Visual Studio 2019. 

To get started, simply download the code and open the Solution.

## Limitations

Unlike Scidvspc this version is only developed to work under 64 bit Windows.

It is also much more limited in intended functionality. For example, it does not provide facilities to play against an engine, play on the internet, work with tablebases or opening books...