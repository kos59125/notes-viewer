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
      // highlight
      document.querySelectorAll("pre").forEach((e, index) => {
        // brefore: <pre></pre>
        // after: 
        // <div class="codeblock">
        //   <pre id="codeblock-{id}"></pre>
        //   <button class="button is-small is-text trigger-clipboard" data-clipboard-target="#codeblock-{index}">
        //     Copy
        //   </button>
        // </div>
        const wrapper = document.createElement("div");
        wrapper.classList.add("codeblock");

        e.id = e.id || `codeblock-${index}`;
        e.parentNode.insertBefore(wrapper, e);
        wrapper.appendChild(e);

        const button = document.createElement("button");
        button.classList.add("button", "is-small", "is-text", "trigger-clipboard");
        button.dataset.clipboardTarget = `#${e.id}`;
        wrapper.appendChild(button);

        const copyText = document.createTextNode("Copy");
        button.appendChild(copyText);
      });
      document.querySelectorAll("pre code").forEach(hljs.highlightBlock);
      document.querySelectorAll("code.hljs").forEach(hljs.lineNumbersBlock);

      // math
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
