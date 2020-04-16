window.RecycleBin = {
  Notes: {
    highlight: () => {
      document.querySelectorAll("pre code").forEach(hljs.highlightBlock);
      document.querySelectorAll(".math").forEach(MathJax.typeset);
    },
  }
};
