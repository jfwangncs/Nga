<template>
  <div class="topic-list-page">
    <AppHeader />

    <PullRefresh :onRefresh="handleRefresh">
      <main class="main-content">
        <!-- 错误提示 -->
        <div v-if="errorMessage" class="error-toast">
          <span class="error-icon">⚠️</span>
          <span class="error-text">{{ errorMessage }}</span>
          <button class="error-close" @click="errorMessage = ''">×</button>
        </div>

        <div class="search-bar">
          <span class="search-icon">🔍</span>
          <input
            v-model="searchKey"
            type="text"
            placeholder="搜索..."
            @keyup.enter="handleSearch"
          />
        </div>

        <div class="catalog-filters">
          <button
            class="catalog-btn"
            :class="{ active: catalog === 'DaXuanWo' }"
            @click="selectCatalog('DaXuanWo')"
          >
            <span class="catalog-icon">🌀</span>
            <span>大漩涡</span>
          </button>
          <button
            class="catalog-btn"
            :class="{ active: catalog === 'Cosplay' }"
            @click="selectCatalog('Cosplay')"
          >
            <span class="catalog-icon">🎭</span>
            <span>Cosplay</span>
          </button>
        </div>

        <div class="topic-list">
          <div
            v-for="topic in topics"
            :key="topic.id"
            class="topic-card"
            @click="goToDetail(topic.tid)"
          >
            <div class="topic-header">
              <div class="topic-left">
                <UserAvatar
                  :avatar="topic.avatar"
                  :username="topic.userName || topic.uid || '用户'"
                  size="medium"
                />
                <div class="topic-info">
                  <div class="topic-author">
                    {{ topic.userName || topic.uid || "用户" }}
                  </div>
                  <div class="topic-time">{{ formatTime(topic.postDate) }}</div>
                </div>
              </div>
              <div class="topic-stats">
                <span>{{ topic.replies || 0 }}条回复</span>
              </div>
            </div>

            <h3 class="topic-title">{{ topic.title }}</h3>

            <div class="topic-tags" v-if="topic.fid">
              <span class="tag">{{ topic.fid }}</span>
            </div>
          </div>
        </div>

        <div class="pagination">
          <button
            class="pagination-btn"
            :disabled="pageIndex <= 1"
            @click="changePage(pageIndex - 1)"
          >
            上一页
          </button>

          <template v-for="page in pageNumbers" :key="page">
            <button
              v-if="page !== '...'"
              class="pagination-num"
              :class="{ active: pageIndex === page }"
              @click="changePage(page)"
            >
              {{ page }}
            </button>
            <span v-else class="pagination-ellipsis">...</span>
          </template>

          <button
            class="pagination-btn"
            :disabled="pageIndex >= totalPages"
            @click="changePage(pageIndex + 1)"
          >
            下一页
          </button>
        </div>
      </main>
    </PullRefresh>

    <AppFooter />
  </div>
</template>

<script setup>
import { ref, onMounted, computed, watch } from "vue";
import { useRouter, useRoute } from "vue-router";
import { getTopicList } from "@/api/topic";
import AppHeader from "@/components/AppHeader.vue";
import AppFooter from "@/components/AppFooter.vue";
import UserAvatar from "@/components/UserAvatar.vue";
import PullRefresh from "@/components/PullRefresh.vue";

const pageNumbers = computed(() => {
  const total = totalPages.value;
  const current = pageIndex.value;
  const pages = [];

  // 如果总页数<=5，显示全部
  if (total <= 5) {
    for (let i = 1; i <= total; i++) {
      pages.push(i);
    }
    return pages;
  }

  // 总页数>5时
  // 始终显示第1页
  pages.push(1);

  // 如果当前页离第1页很近（<=3），显示前4页
  if (current <= 3) {
    pages.push(2, 3, 4);
    pages.push("...");
    pages.push(total);
  }
  // 如果当前页离最后一页很近（>=total-2），显示后4页
  else if (current >= total - 2) {
    pages.push("...");
    pages.push(total - 3, total - 2, total - 1, total);
  }
  // 当前页在中间
  else {
    pages.push("...");
    pages.push(current - 1, current, current + 1);
    pages.push("...");
    pages.push(total);
  }

  return pages;
});

const router = useRouter();
const route = useRoute();

const topics = ref([]);
const pageIndex = ref(Number(route.query.page) || 1);
const pageSize = ref(10);
const totalCount = ref(0);
const searchKey = ref(route.query.searchKey || "");
const catalog = ref(route.query.catalog || "All");
const errorMessage = ref("");

const totalPages = computed(() => {
  return Math.ceil(totalCount.value / pageSize.value);
});

const fetchTopics = async () => {
  try {
    const data = await getTopicList({
      PageIndex: pageIndex.value,
      PageSize: pageSize.value,
      SearchKey: searchKey.value,
      Catalog: catalog.value,
    });

    if (data) {
      topics.value = data.data || [];
      totalCount.value = data.totalCount || 0;
    }
  } catch (error) {
    console.error("Failed to fetch topics:", error);
    errorMessage.value =
      error?.response?.data?.message ||
      error?.message ||
      "加载失败，请稍后重试";
    setTimeout(() => {
      errorMessage.value = "";
    }, 5000);
  }
};

// 监听路由变化，自动同步参数和刷新
watch(
  () => route.query,
  (newQuery) => {
    pageIndex.value = Number(newQuery.page) || 1;
    searchKey.value = newQuery.searchKey || "";
    catalog.value = newQuery.catalog || "All";
    fetchTopics();
  },
);

const handleSearch = () => {
  router.push({
    path: "/",
    query: {
      ...route.query,
      page: 1,
      searchKey: searchKey.value,
      catalog: catalog.value,
    },
  });
};

const selectCatalog = (selectedCatalog) => {
  const newCatalog =
    catalog.value === selectedCatalog ? "All" : selectedCatalog;
  router.push({
    path: "/",
    query: {
      ...route.query,
      page: 1,
      searchKey: searchKey.value,
      catalog: newCatalog,
    },
  });
};

const changePage = (page) => {
  router.push({
    path: "/",
    query: {
      ...route.query,
      page,
      searchKey: searchKey.value,
      catalog: catalog.value,
    },
  });
  window.scrollTo({ top: 0, behavior: "smooth" });
};

const goToDetail = (tid) => {
  router.push({
    path: `/topic/${tid}`,
    query: {
      page: 1,
      returnPage: pageIndex.value,
      returnSearchKey: searchKey.value,
      returnCatalog: catalog.value,
    },
  });
};

const formatTime = (dateStr) => {
  if (!dateStr) return "未知时间";
  try {
    let timestamp = parseInt(dateStr);
    // 如果是秒级时间戳（10位），转换为毫秒
    if (timestamp < 10000000000) {
      timestamp = timestamp * 1000;
    }
    const date = new Date(timestamp);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    const hours = String(date.getHours()).padStart(2, "0");
    const minutes = String(date.getMinutes()).padStart(2, "0");
    const seconds = String(date.getSeconds()).padStart(2, "0");
    return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
  } catch {
    return dateStr;
  }
};

const formatViews = (views) => {
  if (!views) return "0";
  const num = parseInt(views);
  if (num > 1000) {
    return (num / 1000).toFixed(1) + "K";
  }
  return num.toString();
};

// 下拉刷新处理
const handleRefresh = async () => {
  await fetchTopics();
};

onMounted(() => {
  fetchTopics();
});
</script>

<style scoped>
.topic-list-page {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f8f9fa;
}

.main-content {
  max-width: 1440px;
  width: 100%;
  margin: 0 auto;
  padding: 32px;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 20px;
  position: relative;
}

.error-toast {
  position: fixed;
  top: 80px;
  left: 50%;
  transform: translateX(-50%);
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px 24px;
  background: linear-gradient(135deg, #ff6b6b 0%, #ee5a6f 100%);
  color: #ffffff;
  border-radius: 12px;
  box-shadow: 0 8px 24px rgba(255, 107, 107, 0.4);
  z-index: 1000;
  animation: slideDown 0.3s ease-out;
  max-width: 90%;
  word-break: break-word;
}

@keyframes slideDown {
  from {
    opacity: 0;
    transform: translateX(-50%) translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateX(-50%) translateY(0);
  }
}

.error-icon {
  font-size: 20px;
  flex-shrink: 0;
}

.error-text {
  flex: 1;
  font-size: 14px;
  font-weight: 500;
}

.error-close {
  background: rgba(255, 255, 255, 0.2);
  border: none;
  color: #ffffff;
  font-size: 20px;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  line-height: 1;
  transition: background 0.3s;
  flex-shrink: 0;
}

.error-close:hover {
  background: rgba(255, 255, 255, 0.3);
}

.search-bar {
  width: 100%;
  height: 56px;
  background: #ffffff;
  border-radius: 28px;
  padding: 12px 24px;
  display: flex;
  align-items: center;
  gap: 12px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
  border: 2px solid transparent;
  transition: all 0.3s;
}

.search-bar:focus-within {
  border-color: #4a90e2;
  box-shadow: 0 4px 20px rgba(93, 173, 226, 0.2);
}

.search-icon {
  font-size: 18px;
  color: #4a90e2;
}

.search-bar input {
  flex: 1;
  border: none;
  outline: none;
  font-size: 15px;
  color: #333333;
  background: transparent;
}

.search-bar input::placeholder {
  color: #999999;
}

.catalog-filters {
  display: flex;
  gap: 12px;
  justify-content: center;
  margin-bottom: 8px;
}

.catalog-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 20px;
  background: #ffffff;
  border: 2px solid #e0e0e0;
  border-radius: 20px;
  font-size: 14px;
  font-weight: 500;
  color: #666666;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}

.catalog-btn:hover {
  border-color: #4a90e2;
  color: #4a90e2;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(93, 173, 226, 0.2);
}

.catalog-btn.active {
  background: linear-gradient(135deg, #5dade2 0%, #3498db 100%);
  border-color: transparent;
  color: #ffffff;
  box-shadow: 0 4px 16px rgba(93, 173, 226, 0.3);
}

.catalog-btn.active:hover {
  transform: translateY(-2px) scale(1.05);
}

.catalog-icon {
  font-size: 18px;
  line-height: 1;
}

.pagination-info {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 14px;
  color: #666666;
}

.topic-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.topic-card {
  background: linear-gradient(to bottom, #ffffff 0%, #fafafa 100%);
  border-radius: 12px;
  padding: 20px;
  cursor: pointer;
  transition: all 0.3s ease;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
  border: 1px solid rgba(0, 0, 0, 0.05);
  position: relative;
  overflow: hidden;
}

.topic-card::before {
  content: "";
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 3px;
  background: linear-gradient(135deg, #5dade2 0%, #3498db 100%);
  opacity: 0;
  transition: opacity 0.3s;
}

.topic-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 8px 24px rgba(93, 173, 226, 0.2);
  border-color: rgba(93, 173, 226, 0.3);
}

.topic-card:hover::before {
  opacity: 1;
}

.topic-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.topic-left {
  display: flex;
  gap: 12px;
  align-items: flex-start;
}

.topic-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.topic-author {
  font-size: 14px;
  font-weight: 600;
  color: #333333;
}

.topic-time {
  font-size: 12px;
  color: #999999;
}

.topic-stats {
  display: flex;
  gap: 16px;
  font-size: 12px;
  color: #666666;
}

.topic-title {
  font-size: 16px;
  font-weight: 600;
  color: #1a1a1a;
  margin-bottom: 12px;
  line-height: 1.5;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  text-overflow: ellipsis;
}

.topic-card:hover .topic-title {
  color: #4a90e2;
}

.topic-tags {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.tag {
  padding: 4px 12px;
  background: linear-gradient(
    135deg,
    rgba(93, 173, 226, 0.1) 0%,
    rgba(52, 152, 219, 0.1) 100%
  );
  color: #4a90e2;
  font-size: 12px;
  border-radius: 12px;
  font-weight: 500;
  border: 1px solid rgba(93, 173, 226, 0.2);
}

.pagination-ellipsis {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 36px;
  height: 36px;
  font-size: 14px;
  color: #999999;
  user-select: none;
}
.pagination {
  display: flex;
  justify-content: center;
  gap: 8px;
  margin-top: auto;
  padding: 24px 0;
}

.pagination-btn,
.pagination-num {
  min-width: 80px;
  height: 36px;
  padding: 0 16px;
  background: #ffffff;
  border-radius: 8px;
  font-size: 14px;
  color: #666666;
  transition: all 0.3s;
  border: 1px solid #e0e0e0;
}

.pagination-btn:hover:not(:disabled) {
  background: linear-gradient(135deg, #5dade2 0%, #3498db 100%);
  color: #ffffff;
  border-color: transparent;
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(93, 173, 226, 0.3);
}

.pagination-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  background: #f5f5f5;
}

.pagination-num {
  min-width: 36px;
  padding: 0;
}

.pagination-num:hover {
  background: linear-gradient(
    135deg,
    rgba(93, 173, 226, 0.1) 0%,
    rgba(52, 152, 219, 0.1) 100%
  );
  color: #4a90e2;
  border-color: #4a90e2;
}

.pagination-num.active {
  background: linear-gradient(135deg, #5dade2 0%, #3498db 100%);
  color: #ffffff;
  font-weight: 600;
  border-color: transparent;
  box-shadow: 0 4px 12px rgba(93, 173, 226, 0.3);
}
</style>
