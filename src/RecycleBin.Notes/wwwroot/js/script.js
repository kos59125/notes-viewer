window.RecycleBin = {
  Notes: {
    highlight: () => {
      document.querySelectorAll("pre code").forEach(hljs.highlightBlock);
      document.querySelectorAll(".math").forEach(MathJax.typeset);
    },

    smoothScroll: () => {
      document.querySelectorAll("a[href^='#']").forEach((anchor) => {
        anchor.addEventListener("click", (e) => {
          e.preventDefault();
          const target = document.getElementById(anchor.getAttribute("href").substring(1));
          if (target) {
            target.scrollIntoView({ behavior: "smooth" });
          }
        })
      })
    },
  }
};
