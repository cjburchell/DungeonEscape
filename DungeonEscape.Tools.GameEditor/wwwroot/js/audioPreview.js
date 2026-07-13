window.dungeonEscapeAudioPreview = (() => {
    let audio = null;
    let currentUrl = null;

    function stop() {
        if (audio) {
            audio.pause();
            audio.currentTime = 0;
        }

        currentUrl = null;
    }

    async function play(url) {
        if (!url) {
            stop();
            return false;
        }

        if (!audio) {
            audio = new Audio();
        }

        if (currentUrl !== url) {
            audio.pause();
            audio.src = url;
            audio.loop = false;
            currentUrl = url;
        }

        audio.currentTime = 0;
        await audio.play();
        return true;
    }

    return { play, stop };
})();