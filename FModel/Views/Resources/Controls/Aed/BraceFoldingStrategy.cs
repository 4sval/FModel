using System.Collections.Generic;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace FModel.Views.Resources.Controls
{
	public class JsonFoldingStrategies
	{
		private readonly BraceFoldingStrategy _strategy;
		private readonly FoldingManager _foldingManager;

		public JsonFoldingStrategies(TextEditor avalonEditor)
		{
			_foldingManager = FoldingManager.Install(avalonEditor.TextArea);
			_strategy = new BraceFoldingStrategy(avalonEditor);
		}

		public void UpdateFoldings(TextDocument document)
		{
			_foldingManager.UpdateFoldings(_strategy.UpdateFoldings(document), -1);
		}

		public void UnfoldAll()
		{
			if (_foldingManager.AllFoldings == null)
				return;

			foreach (var folding in _foldingManager.AllFoldings)
			{
				folding.IsFolded = false;
			}
		}
		
		public void FoldToggle(int offset)
		{
			if (_foldingManager.AllFoldings == null)
				return;

			var foldSection = _foldingManager.GetFoldingsContaining(offset);
			if (foldSection.Count > 0)
				foldSection[^1].IsFolded = !foldSection[^1].IsFolded;
		}

		public void FoldAtLevel(int level = 0)
		{
			if (_foldingManager.AllFoldings == null)
				return;

			foreach (var folding in _foldingManager.AllFoldings)
			{
				if (folding.Tag is not CustomNewFolding realFolding) continue;
				if (realFolding.Level == level) folding.IsFolded = true;
			}
		}
	}

	public class BraceFoldingStrategy
	{
	    public BraceFoldingStrategy(TextEditor editor)
	    {
		    UpdateFoldings(editor.Document);
	    }

	    public IEnumerable<CustomNewFolding> UpdateFoldings(TextDocument document)
		{
			return CreateNewFoldings(document);
		}

		public IEnumerable<CustomNewFolding> CreateNewFoldings(ITextSource document)
		{
			var newFoldings = new List<CustomNewFolding>();
			var startOffsets = new Stack<int>();
			var lastNewLineOffset = 0;
			var level = -1;
			
			for (var i = 0; i < document.TextLength; i++)
			{
				var c = document.GetCharAt(i);
				switch (c)
				{
					case '{' or '[':
						level++;
						startOffsets.Push(i);
						break;
					case '}' or ']' when startOffsets.Count > 0:
					{
						var startOffset = startOffsets.Pop();
						if (startOffset < lastNewLineOffset)
						{
							newFoldings.Add(new CustomNewFolding(startOffset, i + 1, level));
						}
						level--;
						break;
					}
					case '\n' or '\r':
						lastNewLineOffset = i + 1;
						break;
				}
			}
			
			newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
			return newFoldings;
		}
    }

	public class CustomNewFolding : NewFolding
	{
		public int Level { get; }
		
		public CustomNewFolding(int start, int end, int level) : base(start, end)
		{
			Level = level;
		}

		public override string ToString()
		{
			return $"[{Level}] {StartOffset} -> {EndOffset}";
		}
	}
}