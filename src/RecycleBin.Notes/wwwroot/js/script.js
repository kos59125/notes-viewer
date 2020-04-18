window.RecycleBin = {
  Notes: {
    setTitle: (title) => {
      document.title = title;
    },

    setIcon: (url, type) => {
      const head = document.getElementsByTagName("head")[0];
      const icon = document.createElement("link");
      icon.rel = "icon";
      icon.href = url;
      icon.type = type;

      const existingIcon = head.querySelector("head link[rel='icon']");
      if (existingIcon) {
        head.removeChild(existingIcon);
      }
      head.appendChild(icon);
    },

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
