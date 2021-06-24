using System.Collections.Generic;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace FModel.Views.Resources.Controls
{
	/// <summary>
	/// https://github.com/JTranOrg/JTranEdit/blob/master/JTranEdit/Classes/BraceFoldingStrategy.cs
	/// </summary>
	public interface IFoldingStrategy
	{
		IEnumerable<NewFolding> UpdateFoldings(TextDocument document);
		void CollapseAll();
		void ExpandAll();
	}

	public class JsonFoldingStrategies : IFoldingStrategy
	{
		private readonly List<IFoldingStrategy> _strategies = new();
		private readonly FoldingManager _foldingManager;
		private readonly IComparer<NewFolding> _comparer = new FoldingComparer();

		public JsonFoldingStrategies(TextEditor avalonEditor)
		{
			_foldingManager = FoldingManager.Install(avalonEditor.TextArea);
			_strategies.Add(new BraceFoldingStrategy(avalonEditor, '{', '}'));
			_strategies.Add(new BraceFoldingStrategy(avalonEditor, '[', ']'));
		}

		public IEnumerable<NewFolding> UpdateFoldings(TextDocument document)
		{
			var foldings = new List<NewFolding>();
			foreach (var strategy in _strategies)
			{
				foldings.AddRange(strategy.UpdateFoldings(document));
			}

			foldings.Sort(_comparer);
			_foldingManager.UpdateFoldings(foldings, -1);

			return foldings;
		}

		public void CollapseAll()
		{
			if (_foldingManager.AllFoldings == null)
				return;

			foreach (var folding in _foldingManager.AllFoldings)
			{
				folding.IsFolded = true;
			}

			// Unfold the first fold (if any) to give a useful overview on content
			var foldSection = _foldingManager.GetNextFolding(0);
			if (foldSection != null)
				foldSection.IsFolded = false;
		}

		public void ExpandAll()
		{
			if (_foldingManager.AllFoldings == null)
				return;

			foreach (var folding in _foldingManager.AllFoldings)
			{
				folding.IsFolded = false;
			}
		}

		private class FoldingComparer : IComparer<NewFolding>
		{
			public int Compare(NewFolding x, NewFolding y)
			{
				return x.StartOffset.CompareTo(y.StartOffset);
			}
		}
	}

	public class BraceFoldingStrategy : IFoldingStrategy
	{
		private readonly char _opening;
		private readonly char _closing;
	    
	    public BraceFoldingStrategy(TextEditor editor, char o, char c)
	    {
		    _opening = o;
		    _closing = c;
		    UpdateFoldings(editor.Document);
	    }

	    public IEnumerable<NewFolding> UpdateFoldings(TextDocument document)
		{
			return CreateNewFoldings(document);
		}

		public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
		{
			var newFoldings = new List<NewFolding>();
			var startOffsets = new Stack<int>();
			var lastNewLineOffset = 0;
			
			for (var i = 0; i < document.TextLength; i++)
			{
				var c = document.GetCharAt(i);
				if (c == _opening)
				{
					startOffsets.Push(i);
				}
				else if (c == _closing && startOffsets.Count > 0)
				{
					var startOffset = startOffsets.Pop();
					if (startOffset < lastNewLineOffset)
					{
						newFoldings.Add(new NewFolding(startOffset, i + 1));
					}
				}
				else if (c is '\n' or '\r')
				{
					lastNewLineOffset = i + 1;
				}
			}
			
			newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
			return newFoldings;
		}

		public void CollapseAll()
		{
			throw new System.NotImplementedException();
		}

		public void ExpandAll()
		{
			throw new System.NotImplementedException();
		}
    }
}