/* =============================================================
   BIZFLOW — Modern JS (Dark Mode, Sidebar, Animations)
   ============================================================= */

document.addEventListener('DOMContentLoaded', function () {

  // -----------------------------------------------
  // 1. DARK MODE TOGGLE
  // -----------------------------------------------
  const savedTheme = localStorage.getItem('fm-theme') || 'light';
  document.documentElement.setAttribute('data-bs-theme', savedTheme);
  updateDarkModeIcon(savedTheme);

  const darkToggle = document.getElementById('darkModeToggle');
  if (darkToggle) {
    darkToggle.addEventListener('click', function () {
      const current = document.documentElement.getAttribute('data-bs-theme');
      const next = current === 'dark' ? 'light' : 'dark';
      document.documentElement.setAttribute('data-bs-theme', next);
      localStorage.setItem('fm-theme', next);
      updateDarkModeIcon(next);
    });
  }

  function updateDarkModeIcon(theme) {
    const icon = document.getElementById('darkModeIcon');
    if (icon) {
      icon.className = theme === 'dark' ? 'fa-solid fa-sun' : 'fa-solid fa-moon';
    }
  }

  // -----------------------------------------------
  // 2. ADMIN SIDEBAR TOGGLE (Mobile)
  // -----------------------------------------------
  const sidebarToggle = document.getElementById('sidebarToggle');
  const sidebar = document.getElementById('adminSidebar');
  const sidebarOverlay = document.getElementById('sidebarOverlay');

  if (sidebarToggle && sidebar) {
    sidebarToggle.addEventListener('click', function () {
      sidebar.classList.toggle('show');
      if (sidebarOverlay) sidebarOverlay.classList.toggle('show');
    });
  }

  if (sidebarOverlay) {
    sidebarOverlay.addEventListener('click', function () {
      sidebar.classList.remove('show');
      sidebarOverlay.classList.remove('show');
    });
  }

  // -----------------------------------------------
  // 3. LOADING BUTTON ON FORM SUBMIT
  // -----------------------------------------------
  document.querySelectorAll('form[data-loading]').forEach(function (form) {
    form.addEventListener('submit', function () {
      const btn = form.querySelector('[type="submit"]');
      if (btn && !btn.classList.contains('btn-loading')) {
        btn.classList.add('btn-loading');
        btn.disabled = true;
      }
    });
  });

  // -----------------------------------------------
  // 4. AUTO-DISMISS TOASTS
  // -----------------------------------------------
  document.querySelectorAll('.fm-toast .alert').forEach(function (toast) {
    setTimeout(function () {
      toast.style.transition = 'opacity 0.5s, transform 0.5s';
      toast.style.opacity = '0';
      toast.style.transform = 'translateX(50px)';
      setTimeout(function () { toast.parentElement.remove(); }, 500);
    }, 4000);
  });

  // -----------------------------------------------
  // 5. COUNTER ANIMATION
  // -----------------------------------------------
  document.querySelectorAll('.count-up').forEach(function (el) {
    const target = parseFloat(el.getAttribute('data-target')) || 0;
    const prefix = el.getAttribute('data-prefix') || '';
    const suffix = el.getAttribute('data-suffix') || '';
    const decimals = el.getAttribute('data-decimals') ? parseInt(el.getAttribute('data-decimals')) : 0;
    const duration = 1200;
    const start = 0;
    const startTime = performance.now();

    function update(currentTime) {
      const elapsed = currentTime - startTime;
      const progress = Math.min(elapsed / duration, 1);
      // easeOutExpo
      const eased = progress === 1 ? 1 : 1 - Math.pow(2, -10 * progress);
      const current = start + (target - start) * eased;
      el.textContent = prefix + current.toFixed(decimals) + suffix;
      if (progress < 1) requestAnimationFrame(update);
    }
    requestAnimationFrame(update);
  });

  // -----------------------------------------------
  // 6. ACTIVE SIDEBAR LINK
  // -----------------------------------------------
  const currentPath = window.location.pathname.toLowerCase();
  document.querySelectorAll('.sidebar-nav-link').forEach(function (link) {
    const href = link.getAttribute('href');
    if (href && currentPath.startsWith(href.toLowerCase())) {
      link.classList.add('active');
    }
  });

  // -----------------------------------------------
  // 7. CONFIRM DELETE MODAL
  // -----------------------------------------------
  document.querySelectorAll('[data-confirm-delete]').forEach(function (btn) {
    btn.addEventListener('click', function (e) {
      e.preventDefault();
      const url = btn.getAttribute('href') || btn.getAttribute('data-url');
      const name = btn.getAttribute('data-name') || 'this item';

      const modalHtml = `
        <div class="modal fade modal-confirm" id="deleteModal" tabindex="-1">
          <div class="modal-dialog modal-dialog-centered modal-sm">
            <div class="modal-content">
              <div class="modal-body text-center pt-4">
                <div style="font-size:48px;margin-bottom:12px;">⚠️</div>
                <h5 class="fw-bold">Xác nhận xóa</h5>
                <p class="text-muted">Bạn có chắc muốn xóa <strong>${name}</strong>?</p>
              </div>
              <div class="modal-footer">
                <button class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Hủy</button>
                <form action="${url}" method="POST" style="margin:0;">
                    <input name="__RequestVerificationToken" type="hidden" value="${document.querySelector('[name=__RequestVerificationToken]')?.value || ''}" />
                    <button type="submit" class="btn btn-danger btn-sm">Xóa</button>
                </form>
              </div>
            </div>
          </div>
        </div>`;

      // Remove old modal if exists
      const old = document.getElementById('deleteModal');
      if (old) old.remove();

      document.body.insertAdjacentHTML('beforeend', modalHtml);
      const modal = new bootstrap.Modal(document.getElementById('deleteModal'));
      modal.show();
    });
  });


  // -----------------------------------------------
  // 8. RIPPLE EFFECT FOR BUTTONS
  // -----------------------------------------------
  const rippleSelectors = [
    '.btn-accent', '.btn-accent-outline', '.btn-success', '.btn-primary', 
    '.btn-danger', '.hero-btn', '.btn-cart', '.btn-login', '.btn-register', 
    '.table-action-btn', '.filter-pill'
  ].join(', ');
  
  document.querySelectorAll(rippleSelectors).forEach(btn => {
    btn.classList.add('ripple');
    btn.addEventListener('click', function (e) {
      let rect = btn.getBoundingClientRect();
      let circle = document.createElement('span');
      circle.classList.add('ripple-span');
      let diameter = Math.max(rect.width, rect.height);
      let radius = diameter / 2;
      circle.style.width = circle.style.height = `${diameter}px`;
      circle.style.left = `${e.clientX - rect.left - radius}px`;
      circle.style.top = `${e.clientY - rect.top - radius}px`;
      btn.appendChild(circle);
      setTimeout(() => {
        circle.remove();
      }, 600);
    });
  });

  // -----------------------------------------------
  // 9. IMAGE SKELETON LOADING
  // -----------------------------------------------
  document.querySelectorAll('img:not(.no-skeleton)').forEach(img => {
      // Ignore very small images if dimensions are explicitly set
      if(img.getAttribute('width') < 32 || img.width > 0 && img.width < 32) return;
      
      if(!img.complete) {
          img.classList.add('skeleton');
          img.addEventListener('load', () => { img.classList.remove('skeleton'); });
          img.addEventListener('error', () => { img.classList.remove('skeleton'); });
      }
  });

  // -----------------------------------------------
  // 10. TOAST NOTIFICATIONS (Toastify)
  // -----------------------------------------------
  function showToast(message, type) {
      if (!message) return;
      let bgColor = type === 'success' ? '#10b981' : '#ef4444'; // green or red
      let icon = type === 'success' ? 'fa-circle-check' : 'fa-circle-xmark';
      
      Toastify({
          text: `<i class="fa-solid ${icon} me-2"></i> ${message}`,
          duration: 3500,
          close: true,
          gravity: "bottom", // bottom or top
          position: "right", // left, center or right
          escapeMarkup: false,
          style: {
              background: bgColor,
              borderRadius: "8px",
              boxShadow: "0 10px 25px rgba(0,0,0,0.15)",
              color: "#fff",
              fontWeight: "600",
              fontFamily: "'Inter', sans-serif"
          }
      }).showToast();
  }

  if (window.fmToastSuccess) showToast(window.fmToastSuccess, 'success');
  if (window.fmToastError) showToast(window.fmToastError, 'error');

  // -----------------------------------------------
  // 11. AJAX REAL-TIME SEARCH
  // -----------------------------------------------
  const searchInput = document.getElementById('searchInput');
  const searchDropdown = document.getElementById('searchDropdown');
  const searchResults = document.getElementById('searchResults');
  const searchLoading = document.getElementById('searchLoading');
  const searchEmpty = document.getElementById('searchEmpty');
  let searchTimeout = null;

  if (searchInput && searchDropdown) {
      searchInput.addEventListener('input', function () {
          const term = this.value.trim();
          
          if (term.length < 2) {
              searchDropdown.classList.add('d-none');
              return;
          }

          // Show dropdown & loading
          searchDropdown.classList.remove('d-none');
          searchLoading.classList.remove('d-none');
          searchResults.classList.add('d-none');
          searchEmpty.classList.add('d-none');

          clearTimeout(searchTimeout);
          searchTimeout = setTimeout(() => {
              fetch(`/Products/SearchAjax?term=${encodeURIComponent(term)}`)
                  .then(response => response.json())
                  .then(data => {
                      searchLoading.classList.add('d-none');
                      
                      if (data.success && data.data && data.data.length > 0) {
                          // Render results
                          searchResults.innerHTML = '';
                          data.data.forEach(item => {
                              const img = item.image ? `/uploads/${item.image}` : '/images/placeholder.svg';
                              const html = `
                                  <li>
                                      <a href="/Products/Details/${item.id}" class="search-dropdown-item">
                                          <img src="${img}" class="search-dropdown-img" onerror="this.src='/images/placeholder.svg'" />
                                          <div class="search-dropdown-info">
                                              <div class="search-dropdown-name">${item.name}</div>
                                              <div class="search-dropdown-price">${item.price.toLocaleString("vi-VN")} ₫</div>
                                          </div>
                                      </a>
                                  </li>
                              `;
                              searchResults.innerHTML += html;
                          });
                          searchResults.classList.remove('d-none');
                      } else {
                          // Empty state
                          searchEmpty.classList.remove('d-none');
                      }
                  })
                  .catch(err => {
                      console.error("Search error: ", err);
                      searchLoading.classList.add('d-none');
                      searchEmpty.classList.remove('d-none');
                  });
          }, 350); // Debounce 350ms
      });

      // Hide dropdown when clicking outside
      document.addEventListener('click', function (e) {
          if (!searchInput.contains(e.target) && !searchDropdown.contains(e.target)) {
              searchDropdown.classList.add('d-none');
          }
      });
  }

  // -----------------------------------------------
  // 12. AJAX CART LOGIC (Add, Update, Remove)
  // -----------------------------------------------

  // 12.1. Helper for Cart Fetch
  async function cartFetch(url, formData) {
      return fetch(url, {
          method: 'POST',
          body: formData,
          headers: {
              'X-Requested-With': 'XMLHttpRequest'
          }
      }).then(res => res.json());
  }

  // 12.2. Global Add to Cart AJAX
  document.querySelectorAll('.ajax-cart-form').forEach(form => {
      form.addEventListener('submit', async function(e) {
          e.preventDefault();
          const btn = form.querySelector('button[type="submit"]');
          if (btn.disabled) return;
          
          btn.disabled = true;
          const originalContent = btn.innerHTML;
          btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i>';
          
          try {
              const formData = new FormData(form);
              const data = await cartFetch(form.action, formData);
              
              if (data.success) {
                  if (data.redirectUrl) {
                      window.location.href = data.redirectUrl;
                      return;
                  }
                  showToast(data.message, 'success');
                  // Optional: Update a global badge if you add one later
                  const badge = document.querySelector('.cart-badge');
                  if (badge) {
                      badge.textContent = data.itemCount;
                      badge.style.display = data.itemCount > 0 ? 'inline-block' : 'none';
                  }
              } else {
                  showToast(data.message || 'Lỗi khi thêm vào giỏ hàng', 'error');
              }
          } catch (err) {
              showToast('Lỗi kết nối máy chủ', 'error');
          } finally {
              btn.disabled = false;
              btn.innerHTML = originalContent;
          }
      });
  });

  // 12.3. Cart Page Interactivity
  const cartContainer = document.getElementById('cart-container');
  if (cartContainer) {
      cartContainer.addEventListener('submit', async function(e) {
          const form = e.target;
          const isUpdate = form.classList.contains('cart-update-form');
          const isRemove = form.classList.contains('cart-remove-form');
          
          if (!isUpdate && !isRemove) return;
          
          e.preventDefault();
          const btn = form.querySelector('button[type="submit"]');
          if (btn.disabled) return;
          
          const row = form.closest('.cart-item-card');
          
          // Disable UI to prevent spam
          btn.disabled = true;
          row.style.opacity = '0.6';
          
          try {
              const formData = new FormData(form);
              const data = await cartFetch(form.action, formData);
              
              if (data.success) {
                  if (data.removed || isRemove) {
                      // Animate removal
                      row.style.transition = 'all 0.4s ease';
                      row.style.transform = 'translateX(30px)';
                      row.style.opacity = '0';
                      setTimeout(() => {
                          row.remove();
                          // Check if cart is empty after removal
                          if (document.querySelectorAll('.cart-item-card').length === 0) {
                              location.reload(); // Refresh to show empty state UI
                          }
                      }, 400);
                  } else {
                      // Update quantities and line totals
                      const qtyEl = row.querySelector('.cart-item-qty');
                      const lineTotalEl = row.querySelector('.cart-line-total');
                      if (qtyEl) qtyEl.textContent = data.quantity;
                      if (lineTotalEl) lineTotalEl.textContent = data.lineTotal;
                      row.style.opacity = '1';
                  }
                  
                  // Update overall totals
                  const totalPriceEl = document.getElementById('cart-total-price');
                  const totalCountEl = document.getElementById('cart-total-count');
                  if (totalPriceEl) totalPriceEl.textContent = data.cartTotal;
                  if (totalCountEl) totalCountEl.textContent = data.itemCount + ' sản phẩm';

                  if (data.message) showToast(data.message, 'success');
              } else {
                  row.style.opacity = '1';
                  showToast(data.message || 'Lỗi cập nhật giỏ hàng', 'error');
              }
          } catch (err) {
              row.style.opacity = '1';
              showToast('Lỗi kết nối máy chủ', 'error');
          } finally {
              btn.disabled = false;
          }
      });
  }

});
