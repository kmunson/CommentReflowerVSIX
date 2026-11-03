// Comment Reflower Regression Test Main
// Copyright (C) 2004  Ian Nowland
//
// This program is free software; you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the Free Software
// Foundation; either version 2 of the License, or (at your option) any later
// version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY
// WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A
// PARTICULAR PURPOSE. See the GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along with
// this program; if not, write to the Free Software Foundation, Inc., 59 Temple
// Place - Suite 330, Boston, MA 02111-1307, USA.

using CommentReflowerLib;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CommentReflowerTest;

/// <summary>
/// Summary description for Class1.
/// </summary>
class MainClass
{
	static int FileCompare(
		string fileName1,
		string fileName2
	)
	{
		return StreamCompare(new FileStream(fileName1, FileMode.Open), new FileStream(fileName2, FileMode.Open));
	}

	static int StreamCompare(
		Stream stream1,
		Stream stream2
	)
	{
		using StreamReader file1 = new(stream1);
		using StreamReader file2 = new(stream2);
		int lineNum = 0;
		string st1 = file1.ReadLine();
		while (st1 != null)
		{
			string st2 = file2.ReadLine();
			if ((st2 == null) ||
				(st1.CompareTo(st2) != 0))
			{
				return lineNum;
			}
			lineNum++;
			st1 = file1.ReadLine();
		}
		if (file2.ReadLine() != null)
		{
			return lineNum;
		}
		return -1;
	}

	class GetBlockFromPointRegression
	{
		public GetBlockFromPointRegression(
			bool retSuccess,
			int retStartLine,
			int retEndLine,
			int retIndentation,
			string retCommentBlockName
		)
		{
			this.retSuccess = retSuccess;
			this.retStartLine = retStartLine;
			this.retEndLine = retEndLine;
			this.retIndentation = retIndentation;
			this.retCommentBlockName = retCommentBlockName;
		}

		public void DoTest(TestFileWrapper regressionFile, string fileName)
		{
			ParameterSet pset = new()
			{
				mUseTabsToIndent = true
			};

			for (int lineNumber = this.retStartLine; lineNumber <= this.retEndLine; lineNumber++)
			{
				EnvDTE.EditPoint tp1 = regressionFile.CreateEditPoint(null);
				tp1.MoveToLineAndOffset(lineNumber, 1);
				bool retSuccess;
				retSuccess = CommentReflowerObj.GetBlockContainingPoint(
					pset,
					fileName,
					tp1,
					out CommentBlock retBlock,
					out MatchedBlockData retBdata
				);
				if (retSuccess != this.retSuccess)
				{
					throw new System.ApplicationException(
						"Blockpoint Regression test line " + lineNumber.ToString() +
						" does not match expected return value"
					);
				}
				if (retSuccess == false)
				{
					return;
				}
				if (retBdata.mStartLine != this.retStartLine)
				{
					throw new System.ApplicationException(
						"Blockpoint Regression test line " + lineNumber.ToString() +
						" does not match expected start line value"
					);
				}
				if (retBdata.mEndLine != this.retEndLine)
				{
					throw new System.ApplicationException(
						"Blockpoint Regression test line " + lineNumber.ToString() +
						" does not match expected end line value"
					);
				}
				if (retBlock.mName != this.retCommentBlockName)
				{
					throw new System.ApplicationException(
						"Blockpoint Regression test line " + lineNumber.ToString() +
						" does not match expected block name"
					);
				}
				if (retBdata.mIndentation != this.retIndentation)
				{
					throw new System.ApplicationException(
						"Blockpoint Regression test line " + lineNumber.ToString() +
						" does not match expected indentation"
					);
				}
			}
		}
		public bool retSuccess;
		public int retStartLine;
		public int retEndLine;
		public int retIndentation;
		public string retCommentBlockName;
	}

	private static readonly GetBlockFromPointRegression[] mBlockPointValues =
	{
		new( true,  1,  1,  0, "Doxygen C style (/**)"),
		new(false,  2,  4,  0, ""),
		new( true,  5,  5,  4, "Doxygen C style (/**)"),
		new( true,  6, 10,  0, "Doxygen C style (/**)"),
		new( true, 11, 15,  4, "Doxygen C style (/**)"),
		new( true, 21, 21,  0, "C style"),
		new( true, 27, 27,  7, "C style"),
		new( true, 28, 28,  7, "C style"),
		new( true, 34, 35,  0, "C style"),
		new( true, 41, 42,  7, "C style"),
		new( true, 48, 51,  0, "C style function block"),
		new( true, 58, 58,  0, "C++ style"),
		new( true, 60, 60, 11, "C++ style"),
		new( true, 61, 61, 11, "C++ style"),
		new( true, 63, 65,  0, "C++ style"),
		new( true, 67, 68, 11, "C++ style"),
		new( true, 69, 71,  0, "C++ style"),
		new( true, 73, 76,  0, "C++ style function block"),
		new( true, 78, 80, 12, "C++ style function block"),
		new( true, 81, 83, 16, "C++ style function block"),
		new( true, 85, 87, 12, "C++ style function block"),
		new( true, 88, 90, 12, "C++ style function block"),
	};

	static void DoBlockTests()
	{
		string fileName = @"..\..\Input\Regression.cpp";
		TestFileWrapper regressionFile = new(fileName);

		/////////////////////////////////////////
		// first detection tests
		/////////////////////////////////////////
		foreach (GetBlockFromPointRegression val in mBlockPointValues)
		{
			val.DoTest(regressionFile, fileName);
		}

		//////////////////////////////////////////
		// now formatting tests one by one
		//////////////////////////////////////////
		ParameterSet pset = new()
		{
			mUseTabsToIndent = true
		};
		EnvDTE.EditPoint[] tp = new EnvDTE.EditPoint[mBlockPointValues.Length];
		for (int i = 0; i < mBlockPointValues.Length; i++)
		{
			tp[i] = regressionFile.CreateEditPoint(null);
			tp[i].MoveToLineAndOffset(mBlockPointValues[i].retStartLine, 1);
		}
		for (int i = 0; i < tp.Length; i++)
		{
			try
			{
				CommentReflowerObj.WrapBlockContainingPoint(pset, fileName, tp[i]);
			}
			catch (Exception)
			{
				if (mBlockPointValues[i].retSuccess == true)
				{
					throw;
				}
			}
		}
		regressionFile.WriteToFile(@"..\..\Output\Regression.out.cpp");
		int lineNum = FileCompare(@"..\..\Output\Regression.out.cpp", @"..\..\Compare\Regression.compare.cpp");
		if (lineNum != -1)
		{
			throw new System.ApplicationException(
				"One by One Block regression file does not match compare at line " + lineNum
			);
		}

		////////////////////////////////////////
		// now formatting tests all at once
		////////////////////////////////////////
		TestFileWrapper regressionFile2 = new(fileName);
		EnvDTE.EditPoint tp1 = regressionFile2.CreateEditPoint(null);
		tp1.StartOfDocument();
		EnvDTE.EditPoint tp2 = regressionFile2.CreateEditPoint(null);
		tp2.EndOfDocument();
		CommentReflowerObj.WrapAllBlocksInSelection(pset, fileName, tp1, tp2);
		regressionFile2.WriteToFile(@"..\..\Output\Regression.out2.cpp");
		lineNum = FileCompare(@"..\..\Output\Regression.out2.cpp", @"..\..\Compare\Regression.compare.cpp");
		if (lineNum != -1)
		{
			throw new System.ApplicationException(
				"Whole block regression file does not match compare at line " + lineNum
			);
		}

		//now apply again and ensure consitancy
		tp1.StartOfDocument();
		tp2.EndOfDocument();
		CommentReflowerObj.WrapAllBlocksInSelection(pset, fileName, tp1, tp2);
		regressionFile2.WriteToFile(@"..\..\Output\Regression.out3.cpp");
		lineNum = FileCompare(@"..\..\Output\Regression.out3.cpp", @"..\..\Compare\Regression.compare.cpp");
		if (lineNum != -1)
		{
			throw new System.ApplicationException(
				"Whole block second run regression file does not match compare at line " + lineNum
			);
		}

		Console.WriteLine("Block detection and formatting tests PASSED.");
	}

	static void DoSimpleSingleFileTest(
		string testName,
		string fileNamePrefix,
		string fileNameSuffix
		)
	{
		string fileName = @"..\..\Input\" + fileNamePrefix + fileNameSuffix;
		ParameterSet pset = new()
		{
			mUseTabsToIndent = true
		};

		TestFileWrapper regressionFile = new(fileName);
		EnvDTE.EditPoint tp1 = regressionFile.CreateEditPoint(null);
		tp1.StartOfDocument();
		EnvDTE.EditPoint tp2 = regressionFile.CreateEditPoint(null);
		tp2.EndOfDocument();
		CommentReflowerObj.WrapAllBlocksInSelection(pset, fileName, tp1, tp2);
		regressionFile.WriteToFile(@"..\..\Output\" + fileNamePrefix + ".out" + fileNameSuffix);
		int lineNum = FileCompare(
			@"..\..\Output\" + fileNamePrefix + ".out" + fileNameSuffix,
			@"..\..\Compare\" + fileNamePrefix + ".compare" + fileNameSuffix
		);
		if (lineNum != -1)
		{
			throw new System.ApplicationException(
				testName + " regression file does not match compare at line " + lineNum
			);
		}

		//now apply again and ensure consitancy
		tp1.StartOfDocument();
		tp2.EndOfDocument();
		CommentReflowerObj.WrapAllBlocksInSelection(pset, fileName, tp1, tp2);
		regressionFile.WriteToFile(@"..\..\Output\" + fileNamePrefix + ".out2" + fileNameSuffix);
		lineNum = FileCompare(
			@"..\..\Output\" + fileNamePrefix + ".out2" + fileNameSuffix,
			@"..\..\Compare\" + fileNamePrefix + ".compare" + fileNameSuffix
		);
		if (lineNum != -1)
		{
			throw new System.ApplicationException(
				testName + " 2nd run regression file does not match compare at line " + lineNum
			);
		}
		Console.WriteLine(testName + " tests PASSED.");
	}

	static void DoXmlFileTests()
	{
		ParameterSet pset = new();
		MemoryStream memorySteam = pset.WriteToXmlMemoryStream();
		pset = new ParameterSet(memorySteam);
		MemoryStream memoryStream2 = pset.WriteToXmlMemoryStream();
		int lineNum = StreamCompare(memorySteam, memoryStream2);
		if (lineNum != -1)
		{
			throw new System.ApplicationException(
				"2nd output of xml file does not match first at line " + lineNum
			);
		}
		Console.WriteLine("Xml file tests PASSED.");
	}

	/// <summary>
	/// The main entry point for the application.
	/// </summary>
	/// <returns>0 if all the tests pass, or 1 if any fail.</returns>
	[STAThread]
	static int Main()
	{
		int exitCode = 0;
		try
		{
			DoBlockTests();
			DoSimpleSingleFileTest("Blank Lines", "RegressionBlankLines", ".cpp");
			DoSimpleSingleFileTest("Bullets", "RegressionBullets", ".cpp");
			DoSimpleSingleFileTest("Break Flow Strings", "RegressionBreakFlowStrings", ".cpp");
			DoSimpleSingleFileTest("VB", "Regression", ".vb");
			DoSimpleSingleFileTest("Jamfile", "Regression", ".JAM");
			DoSimpleSingleFileTest("Text file", "Regression", ".txt");
			DoXmlFileTests();
		}
		catch (Exception e)
		{
			Console.WriteLine("An exception occurred: " + e.Message + ". Test Failed\n");
			exitCode = 1;
		}

		// Only wait when run from a console window, so the results can be read before it
		// closes. Console.ReadKey throws when input is redirected.
		if (!Console.IsInputRedirected)
		{
			Console.WriteLine("\nPress any key to end this program...");
			Console.ReadKey();
		}

		return exitCode;
	}
}
