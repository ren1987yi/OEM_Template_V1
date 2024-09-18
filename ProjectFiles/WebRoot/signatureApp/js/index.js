function init() {
    const canvas = document.getElementById("canvas");
    const options = {
        width: Math.min(window.innerWidth, 1000),
        height: 600,
        minWidth: 4,
        maxWidth: 12,
        // color: '#1890ff',
        bgColor: '#f6f6f6'
    };
    this.signature = new SmoothSignature(canvas, options);
};

function handleClear() {
    this.signature.clear();
};
function handleUndo() {
    this.signature.undo();
};
function handleColor() {
    this.signature.color = '#' + Math.random().toString(16).slice(-6);
};
function handlePreview() {
    const isEmpty = this.signature.isEmpty();
    if (isEmpty) {
        alert('isEmpty')
        return;
    }
    const pngUrl = this.signature.getPNG();
    window.previewImage(pngUrl);
};


async function handleSubmit(){
    const isEmpty = this.signature.isEmpty();
    if (isEmpty) {
        alert('isEmpty')
        return;
    }
    const png = this.signature.getPNG();
    await postData("./signature/save",{id:"123",image:png});
}

async function postData(url = "", data = {}) {
    // Default options are marked with *
    const response = await fetch(url, {
      method: "POST", // *GET, POST, PUT, DELETE, etc.
    //   mode: "cors", // no-cors, *cors, same-origin
      cache: "no-cache", // *default, no-cache, reload, force-cache, only-if-cached
    //   credentials: "same-origin", // include, *same-origin, omit
      headers: {
        "Content-Type": "application/json",
        // 'Content-Type': 'application/x-www-form-urlencoded',
      },
      redirect: "follow", // manual, *follow, error
      referrerPolicy: "no-referrer", // no-referrer, *no-referrer-when-downgrade, origin, origin-when-cross-origin, same-origin, strict-origin, strict-origin-when-cross-origin, unsafe-url
      body: JSON.stringify(data), // body data type must match "Content-Type" header
    });
    return response.json(); // parses JSON response into native JavaScript objects
  }

window.previewImage = function (url, degree) {
    const img = document.createElement('img')
    img.src = url
    const viewer = new Viewer(img, {
      title: 0,
      navbar: 0,
      toolbar: {
        zoomIn: 1,
        zoomOut: 1,
        oneToOne: 1,
        reset: 1,
        rotateLeft: 1,
        rotateRight: 1,
        flipHorizontal: 1,
        flipVertical: 1,
      },
      viewed() {
        if (degree) {
          this.viewer.zoomTo(0.8).rotateTo(degree);
        }
      }
    })
    img.onload = function() {
      viewer.show()
    }
  }

window.onload = ()=>{
    init();


}