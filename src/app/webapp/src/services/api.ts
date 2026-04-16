/** 后端 API 基地址，开发环境默认 http://localhost:5000，Docker 环境通过 nginx 反代使用空字符串 */
export const API_BASE =
  import.meta.env.VITE_API_BASE ?? "http://localhost:5000";
