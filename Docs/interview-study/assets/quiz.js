/* ============================================================================
   课程答题组件 —— 可复用于所有课程
   ----------------------------------------------------------------------------
   课程里只写 HTML，不写 JS：

     <section class="quiz">
       <div class="q" data-why="这个答案为什么对（或错的为什么错）">
         <p class="q-stem">题干</p>
         <div class="q-opts">
           <button type="button" class="q-opt" data-ok>正确答案</button>
           <button type="button" class="q-opt">干扰项</button>
         </div>
       </div>
     </section>

   行为：点一次即锁定；即时反馈；答错也把正确项标出来；底部实时累计得分。
   不用 data-why 就只给对错，不解释。
   ========================================================================= */

(function () {
  'use strict';

  var LETTERS = ['A', 'B', 'C', 'D', 'E', 'F'];

  function initQuiz(quiz) {
    var questions = Array.prototype.slice.call(quiz.querySelectorAll('.q'));
    if (!questions.length) return;

    var score = document.createElement('p');
    score.className = 'q-score';
    score.setAttribute('aria-live', 'polite');
    quiz.appendChild(score);

    var answered = 0;
    var correct = 0;

    function paint() {
      if (!answered) return;
      var done = answered === questions.length;
      var tail = '';
      if (done) {
        var pct = correct / questions.length;
        tail = correct === questions.length
          ? '　全对——这一课的结构你已经拿住了。'
          : pct >= 0.6
            ? '　骨架有了，把答错那条链回正文再走一遍。'
            : '　先别往下走。回到调用链那张图重读一次，再重做。';
      }
      score.textContent = '已答 ' + answered + '/' + questions.length +
        '　·　答对 ' + correct + tail;
    }

    questions.forEach(function (q) {
      var opts = Array.prototype.slice.call(q.querySelectorAll('.q-opt'));
      var why = q.getAttribute('data-why');

      var whyEl = null;
      if (why) {
        whyEl = document.createElement('p');
        whyEl.className = 'q-why';
        whyEl.hidden = true;
        q.appendChild(whyEl);
      }

      opts.forEach(function (opt, i) {
        opt.setAttribute('data-k', LETTERS[i] || String(i + 1));

        opt.addEventListener('click', function () {
          if (q.dataset.done) return;
          q.dataset.done = '1';
          answered += 1;

          var isRight = opt.hasAttribute('data-ok');
          if (isRight) correct += 1;
          opt.classList.add(isRight ? 'is-ok' : 'is-no');

          opts.forEach(function (o) {
            o.disabled = true;
            if (o !== opt && o.hasAttribute('data-ok')) o.classList.add('is-key');
          });

          if (whyEl) {
            whyEl.innerHTML = (isRight ? '<b>对。</b>' : '<b>不对。</b>') + why;
            whyEl.hidden = false;
          }
          paint();
        });
      });
    });
  }

  function boot() {
    Array.prototype.forEach.call(document.querySelectorAll('.quiz'), initQuiz);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }
})();
