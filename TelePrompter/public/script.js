document.addEventListener("DOMContentLoaded", function () {
    const textInput = document.getElementById("text-input");
    const startBtn = document.getElementById("start-btn");
    const saveBtn = document.getElementById("save-btn");
    const deleteBtn = document.getElementById("delete-btn");
    const resetBtn = document.getElementById("reset-btn");
    const stopBtn = document.getElementById("stop-btn");
    const startpauseBtn = document.getElementById("startpause-btn");
    const fontSizeSelect = document.getElementById("font-size");
    const scrollSpeed = document.getElementById("scroll-speed");
    const wpmSlider = document.getElementById("wpm");
    const teleprompter = document.getElementById("teleprompter");
    const inputSection = document.getElementById("input-section");
    const controls = document.getElementById("controls");
    const fixedStopBtn = document.getElementById("fixed-stop-btn");

    const fontSizeLabel = document.getElementById("font-size-label");
    const scrollSpeedLabel = document.getElementById("scroll-speed-label");
    const wpmLabel = document.getElementById("wpm-label");
    const arrowControls = document.getElementById("arrow-controls");
    const arrowUp = document.getElementById("arrow-up");
    const arrowDown = document.getElementById("arrow-down");

    let words = [];
    let currentWordIndex = 0;
    let scrolling;
    let rolling;
    let recognition;
    let running = false;

    // Save text to local storage
    saveBtn.addEventListener("click", function () {
        const text = textInput.value.trim();
        localStorage.setItem("teleprompterText", text);
        alert("Testo salvato!");
    });

    // Delete text from local storage
    deleteBtn.addEventListener("click", function () {
        localStorage.removeItem("teleprompterText");
        textInput.value = "";
        alert("Testo eliminato!");
    });

    function highlightWordsToNextParagraph(up) {
        const wordsElements = document.querySelectorAll(".word");
        let startIndex = currentWordIndex;
        let endIndex = currentWordIndex;
        if (up) {
            for (let i = currentWordIndex - 1; i >= 0; i--) {
                if (wordsElements[i].textContent.startsWith("\n") || i === 0) { 
                    startIndex = i;
                    break;
                }
            }
            document.querySelectorAll(".word.active").forEach((word, i) => {
                if(i >= startIndex)
                    word.classList.remove("active");
            });
            currentWordIndex = startIndex;
        } else {

            for (let i = currentWordIndex + 2; i < wordsElements.length; i++) {
                if (wordsElements[i].textContent.startsWith("\n") || i === wordsElements.length-1) {
                    endIndex = i === wordsElements.length-1 ? i : i-1;
                    break;
                }
            }
            for (let i = startIndex; i <= endIndex; i++) {
                wordsElements[i].classList.add("active");
            }
            currentWordIndex = endIndex;
        }
        // Scroll to the current word index with an offset of 50px
        const currentWord = wordsElements[currentWordIndex];
        if (currentWord) {
            const topPos = currentWord.getBoundingClientRect().top + window.scrollY;
            window.scrollTo({ top: topPos, behavior: 'smooth' });
        }
    }

    arrowUp.addEventListener("click", function () {
        highlightWordsToNextParagraph(true);
    });

    arrowDown.addEventListener("click", function () {
        highlightWordsToNextParagraph(false);
    });

    // Avvia il teleprompter
    startBtn.addEventListener("click", function () {
        const text = textInput.value.trim();
        if (text === "") return alert("Inserisci un testo!");

         // Replace single newlines with double newlines
         const processedText = text.replace(/([^\n])\n([^\n])/g, '$1\n \n$2');

        words = processedText.split(" ").map(word => `<span class="word">${word}</span>`);
        teleprompter.innerHTML = words.join(" ");
        
        inputSection.classList.add("hidden");
        controls.classList.remove("hidden");
        teleprompter.classList.remove("hidden");
        arrowControls.classList.remove("hidden");

        // startScrolling();
        // startVoiceRecognition();
    });

    // Ferma il teleprompter
    function stopTeleprompter() {
        stopAllActivities();
        inputSection.classList.remove("hidden");
        controls.classList.add("hidden");
        teleprompter.classList.add("hidden");
        arrowControls.classList.add("hidden");
        fixedStopBtn.style.display = "none";
        currentWordIndex = 0;
    }

    function stopAllActivities() {
        clearInterval(scrolling);
        clearInterval(rolling);
        if (recognition) recognition.stop();
        running = false;
    }

    stopBtn.addEventListener("click", stopTeleprompter);
    fixedStopBtn.addEventListener("click", handlePageProgress);

    resetBtn.addEventListener("click", function () {
        stopAllActivities();
        currentWordIndex = 0;
        document.querySelectorAll(".word").forEach((word, i) => {
            word.classList.remove("active");
        });
    });

    startpauseBtn.addEventListener("click", handlePageProgress);

    function handlePageProgress(){
        if(running){
            stopAllActivities();
            return;
        }
        fixedStopBtn.style.display = "block";
        // Scroll to the last active word with an offset of 50px
        const activeWords = document.querySelectorAll(".word.active");
        let lastActiveWord = activeWords[activeWords.length - 1];
        if(!lastActiveWord) {
            lastActiveWord = document.querySelectorAll(".word")[0];
        }
        
        const offset = 50;
        const topPos = lastActiveWord.getBoundingClientRect().top + window.scrollY - offset;
        window.scrollTo({ top: topPos, behavior: 'smooth' });
        
        highlightWords();
        startVoiceRecognition();
        startScrolling();
        running = true;
    }

    // Modifica dimensione del testo
    fontSizeSelect.addEventListener("change", function () {
        teleprompter.style.fontSize = fontSizeSelect.value;
        localStorage.setItem('fontSize', fontSizeSelect.value);
        fontSizeLabel.textContent = fontSizeSelect.value;
    });

    // Funzione per lo scorrimento automatico
    function startScrolling() {
        clearInterval(scrolling);
        scrolling = setInterval(() => {
            document.documentElement.scrollTop += parseInt(scrollSpeed.value);
        }, 100);
    }

    // Cambia colore in base a WPM
    function highlightWords() {
        const wpm = parseInt(wpmSlider.value);
        const interval = 60000 / wpm; // Tempo per parola
        
        clearInterval(rolling);
        rolling = setInterval(() => {
            if (currentWordIndex < words.length) {
                document.querySelectorAll(".word").forEach((word, i) => {
                    word.classList.toggle("active", i <= currentWordIndex);
                });
                currentWordIndex++;
            }
        }, interval);
    }

    // Load saved settings
    if (localStorage.getItem('fontSize')) {
        fontSizeSelect.value = localStorage.getItem('fontSize');
        teleprompter.style.fontSize = fontSizeSelect.value; // Update interface
        fontSizeLabel.textContent = fontSizeSelect.value; // Update label
        fontSizeSelect.dispatchEvent(new Event('change')); // Trigger change event
    }
    if (localStorage.getItem('scrollSpeed')) {
        scrollSpeed.value = localStorage.getItem('scrollSpeed');
        scrollSpeedLabel.textContent = scrollSpeed.value; // Update label
    }
    if (localStorage.getItem('wpm')) {
        wpmSlider.value = localStorage.getItem('wpm');
        wpmLabel.textContent = wpmSlider.value; // Update label
    }

    scrollSpeed.addEventListener('input', () => {
        localStorage.setItem('scrollSpeed', scrollSpeed.value);
        scrollSpeedLabel.textContent = scrollSpeed.value;
    });

    wpmSlider.addEventListener('input', () => {
        localStorage.setItem('wpm', wpmSlider.value);
        wpmLabel.textContent = wpmSlider.value;
    });

    // Load text from local storage
    const savedText = localStorage.getItem("teleprompterText");
    if (savedText) {
        textInput.value = savedText;
        
        startBtn.dispatchEvent(new Event('click')); 
    }
    // Riconoscimento vocale per sincronizzazione
    function startVoiceRecognition() {
        if (!("webkitSpeechRecognition" in window)) {
            alert("Il tuo browser non supporta il riconoscimento vocale.");
            return;
        }

        recognition = new webkitSpeechRecognition();
        recognition.lang = "it-IT";
        recognition.continuous = true;

        recognition.onresult = function (event) {
            const transcript = event.results[event.results.length - 1][0].transcript.trim();
            syncTextWithVoice(transcript);
        };

        recognition.start();
    }

    // Sincronizza il testo con la voce
    function syncTextWithVoice(transcript) {
        const wordsArray = words.map(word => word.replace(/<[^>]+>/g, "").toLowerCase());
        const spokenWords = transcript.toLowerCase().split(" ");
        
        let lastMatchedIndex = -1;
        spokenWords.forEach(spokenWord => {
            if (wordsArray.indexOf(spokenWord, lastMatchedIndex + 1) !== -1) lastMatchedIndex = index;
        });

        if (lastMatchedIndex > currentWordIndex) {
            for (let i = currentWordIndex; i <= lastMatchedIndex; i++) {
                document.querySelectorAll(".word")[i].classList.add("active");
            }
            currentWordIndex = lastMatchedIndex + 1;
        }
    }
});
